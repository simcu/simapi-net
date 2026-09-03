using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Hangfire;
using Hangfire.Console;
using Hangfire.Redis.StackExchange;
using SimApi.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OpenApi;
using SimApi.Middlewares;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SimApi.Attributes;
using SimApi.AuthSDK;
using SimApi.Configurations;
using SimApi.Controllers;
using SimApi.Interfaces;
using SimApi.Logger;
using SimApi.SwaggerFilters;
using StackExchange.Redis;

namespace SimApi;

/// <summary>
/// 加入系统的扩展信息
/// </summary>
public static class SimApiExtensions
{
    //**********快捷添加**************
    public static IServiceCollection AddSimApi(this IServiceCollection builder,
        Action<SimApiOptions>? options = null)
    {
        var simApiOptions = new SimApiOptions();
        options?.Invoke(simApiOptions);
        simApiOptions.WebConfig ??= new();
        builder.AddSingleton(simApiOptions);

        if (simApiOptions.RedisConfiguration != null)
        {
            builder.AddStackExchangeRedisCache(x => x.Configuration = simApiOptions.RedisConfiguration);
            builder.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(simApiOptions.RedisConfiguration));
        }
        else if (simApiOptions.EnableSimApiAuth || simApiOptions.EnableSimApiCache)
        {
            builder.AddDistributedMemoryCache();
        }

        if (simApiOptions.EnableSimApiCache)
        {
            builder.AddSingleton<SimApiCache>();
        }

        if (simApiOptions.EnableLogger)
        {
            builder.AddLogging(logger =>
            {
                logger.ClearProviders();
                logger.AddProvider(new SimApiLoggerProvider());
            });
        }

        // 是否使用 AUTH
        if (simApiOptions.EnableSimApiAuth)
        {
            builder.AddSingleton<SimApiAuth>();
        }

        var simApiAuthChecker = typeof(ISimApiAuthChecker);
        var stackTrace = new StackTrace();
        var callingMethod = stackTrace.GetFrame(stackTrace.FrameCount - 1)?.GetMethod();
        var callerAssembly = callingMethod?.DeclaringType?.Assembly;
        var callerTypes = callerAssembly?.GetTypes() ?? [];

        foreach (var type in callerTypes)
        {
            if (type is { IsClass: true, IsAbstract: false } && simApiAuthChecker.IsAssignableFrom(type))
            {
                builder.AddScoped(simApiAuthChecker, type);
            }
        }

        if (simApiOptions.EnableJob)
        {
            builder.AddHangfire(x =>
            {
                var redisOption = new RedisStorageOptions();
                if (simApiOptions.SimApiJobOptions.Database.HasValue)
                {
                    redisOption.Db = simApiOptions.SimApiJobOptions.Database.Value;
                }

                x.UseRedisStorage(simApiOptions.SimApiJobOptions.RedisConfiguration ??
                                  simApiOptions.RedisConfiguration, redisOption);
                x.UseConsole();
            });
            foreach (var server in simApiOptions.SimApiJobOptions.Servers)
            {
                builder.AddHangfireServer(hfs =>
                {
                    hfs.Queues = server.Queues;
                    hfs.WorkerCount = server.WorkerNum;
                });
            }
        }

        if (simApiOptions.EnableCors)
        {
            builder.AddCors(cors => cors.AddPolicy("any",
                policy => { policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin(); }));
        }

        if (simApiOptions.EnableSimApiHttpClient)
        {
            builder.AddSingleton<SimApiHttpClient>();
        }

        if (simApiOptions.EnableSynapse)
        {
            builder.AddSingleton<Synapse>();
            foreach (var type in callerTypes)
            {
                var methodsWithSynapse = type.GetMethods()
                    .Where(m => m.GetCustomAttribute<SynapseRpcAttribute>() != null ||
                                m.GetCustomAttribute<SynapseEventAttribute>() != null);
                if (!methodsWithSynapse.Any()) continue;
                builder.AddScoped(type);
            }
        }

        // 使用SimApiDoc
        if (simApiOptions.EnableSimApiDoc)
        {
            var docOptions = simApiOptions.SimApiDocOptions;
            builder.AddSwaggerGen(x =>
            {
                foreach (var group in docOptions.ApiGroups)
                {
                    x.SwaggerDoc(group.Id, new OpenApiInfo
                    {
                        Title = group.Name,
                        Description = group.Description
                    });
                }

                x.CustomSchemaIds(type =>
                {
                    // 递归解析类型名称（处理嵌套泛型/数组/可空 + 保证唯一性）
                    string GetSimpleTypeName(Type t, int depth = 0)
                    {
                        // 防止无限递归
                        if (depth > 5) return t.Name.Split('`')[0];

                        // 处理数组类型
                        if (t.IsArray)
                        {
                            var elementType = t.GetElementType();
                            return $"{GetSimpleTypeName(elementType!, depth + 1)}[]";
                        }

                        // 处理可空类型
                        if (Nullable.GetUnderlyingType(t) != null)
                        {
                            var underlyingType = Nullable.GetUnderlyingType(t);
                            return GetSimpleTypeName(underlyingType!, depth + 1);
                        }

                        // 处理泛型类型（递归解析嵌套泛型）
                        if (t.IsGenericType)
                        {
                            var genericBaseName = t.GetGenericTypeDefinition().Name.Split('`')[0];
                            var genericArgs = t.GetGenericArguments()
                                .Select(arg => GetSimpleTypeName(arg, depth + 1))
                                .Where(arg => !string.IsNullOrEmpty(arg))
                                .ToArray();
                            return $"{genericBaseName}<{string.Join(",", genericArgs)}>";
                        }

                        // 处理基础类型（小写）
                        if (t.IsPrimitive || t == typeof(string) || t == typeof(DateTime) || t == typeof(Guid))
                        {
                            return t.Name switch
                            {
                                "String" => "string",
                                "Int32" => "int",
                                "Int64" => "long",
                                "Boolean" => "boolean",
                                "DateTime" => "datetime",
                                "Guid" => "guid",
                                _ => t.Name.ToLower()
                            };
                        }

                        // 核心修复：生成唯一名称（处理嵌套类/同名不同类）
                        var typeName = t.Name;

                        // 步骤1：处理嵌套类（如 ApplicationDto+ApplicationEditRequest → ApplicationDto_ApplicationEditRequest）
                        if (t.DeclaringType != null)
                        {
                            typeName = $"{GetSimpleTypeName(t.DeclaringType)}.{typeName}";
                        }

                        // 步骤2：（可选）处理同命名空间下的同名类（拼接命名空间前缀，避免全局重复）
                        // 如需更严格的唯一性，取消注释下面这行
                        // typeName = $"{t.Namespace?.Replace(".", "_")}_{typeName}";

                        return typeName;
                    }

                    // 根调用：解析当前类型
                    var uniqueSchemaId = GetSimpleTypeName(type);
                    // 可选：移除特殊字符（如 $、+），避免 Swagger 解析问题
                    return uniqueSchemaId.Replace("$", "").Replace("+", "_");
                });
                x.OperationFilter<SimApiResponseOperationFilter>();
                x.OperationFilter<SimApiSignOperationFilter>();
                x.OperationFilter<AesBodyOperationFilter>();
                x.SchemaFilter<GlobalDynamicObjectSchemaFilter>();
                x.SchemaFilter<DictionarySchemaFilter>();
                x.DocumentFilter<RemoveEmptyTagsFilter>();
                if (simApiOptions.EnableSimApiAuth)
                {
                    x.OperationFilter<SimApiAuthOperationFilter>();
                }

                x.DocInclusionPredicate((docName, apiDesc) =>
                {
                    var metadata = apiDesc.ActionDescriptor.EndpointMetadata;

                    // 类/方法上的 [SimApiDoc] 标注统一控制文档归属
                    var docAttrs = metadata.OfType<SimApiDocAttribute>().ToArray();

                    // Ignore: 不出现在任何文档(路由不受影响)
                    if (docAttrs.Any(a => a.Ignore))
                    {
                        return false;
                    }

                    // GroupNames: 逗号分隔的文档组列表; "*" 表示所有文档;
                    // 标注在类上则整类生效, 方法级标注可覆盖
                    var groupNames = docAttrs
                        .Select(a => a.GroupNames)
                        .FirstOrDefault(g => !string.IsNullOrWhiteSpace(g));
                    if (groupNames != null)
                    {
                        var groups = groupNames.Split(',',
                            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        return groups.Contains(SimApiDocAttribute.AllGroups) || groups.Contains(docName);
                    }

                    // 兼容: ApiExplorerSettings.GroupName 单组标注
                    var actionGroupName = metadata
                        .OfType<ApiExplorerSettingsAttribute>()
                        .FirstOrDefault()?.GroupName;

                    // 未标注任何分组 → 放入配置为 IsDefault 的文档组;
                    // 未配置 IsDefault 时依次回退 id 为 "api" 的组 / 第一个组
                    if (actionGroupName == null)
                    {
                        var defaultGroup = docOptions.ApiGroups.FirstOrDefault(g => g.IsDefault)
                                           ?? docOptions.ApiGroups.FirstOrDefault(g => g.Id == "api")
                                           ?? docOptions.ApiGroups.FirstOrDefault();
                        return defaultGroup != null && docName == defaultGroup.Id;
                    }

                    return docName == actionGroupName;
                });

                x.EnableAnnotations();
                var haveOauth = false;
                var oauthFlows = new OpenApiOAuthFlows();
                foreach (var auth in docOptions.ApiAuth.Type)
                {
                    switch (auth)
                    {
                        case "SimApiAuth":
                            x.AddSecurityDefinition("SimApiAuth",
                                new OpenApiSecurityScheme
                                {
                                    Name = "Token",
                                    In = ParameterLocation.Header,
                                    Type = SecuritySchemeType.ApiKey
                                });
                            break;
                        case "ClientCredentials":
                            oauthFlows.ClientCredentials = new OpenApiOAuthFlow
                            {
                                TokenUrl = new Uri(docOptions.ApiAuth.TokenUrl, UriKind.RelativeOrAbsolute),
                                Scopes = docOptions.ApiAuth.Scopes
                            };
                            haveOauth = true;
                            break;
                        case "Implicit":
                            oauthFlows.Implicit = new OpenApiOAuthFlow
                            {
                                AuthorizationUrl = new Uri(docOptions.ApiAuth.AuthorizationUrl,
                                    UriKind.RelativeOrAbsolute),
                                Scopes = docOptions.ApiAuth.Scopes
                            };
                            haveOauth = true;
                            break;
                        case "AuthorizationCode":
                            oauthFlows.AuthorizationCode = new OpenApiOAuthFlow
                            {
                                TokenUrl = new Uri(docOptions.ApiAuth.TokenUrl, UriKind.RelativeOrAbsolute),
                                AuthorizationUrl = new Uri(docOptions.ApiAuth.AuthorizationUrl,
                                    UriKind.RelativeOrAbsolute),
                                Scopes = docOptions.ApiAuth.Scopes
                            };
                            haveOauth = true;
                            break;
                        case "Password":
                            oauthFlows.Password = new OpenApiOAuthFlow
                            {
                                TokenUrl = new Uri(docOptions.ApiAuth.TokenUrl, UriKind.RelativeOrAbsolute),
                                Scopes = docOptions.ApiAuth.Scopes
                            };
                            haveOauth = true;
                            break;
                    }
                }

                if (haveOauth)
                {
                    x.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.OAuth2,
                        Flows = oauthFlows,
                        Description = docOptions.ApiAuth.Description,
                        In = ParameterLocation.Header
                    });
                }
            });

            // 让 SimApiRouteOptions 配置的内置约定路由(UserInfo/Logout/WebConfig)
            // 也能出现在文档中: 通过 IApiDescriptionProvider 手工产出 ApiDescription,
            // 请求/响应 schema 由 Swashbuckle 自动分析, [SimApiDoc] 注解自动生效。
            builder.TryAddEnumerable(
                ServiceDescriptor.Transient<IApiDescriptionProvider,
                    SimApiBuiltInRoutesDescriptionProvider>());
        }

        // 使用Header转发，应对代理后获取真实ip
        if (simApiOptions.EnableForwardHeaders)
        {
            builder.Configure<ForwardedHeadersOptions>(fwOptions =>
            {
                fwOptions.ForwardedHeaders = ForwardedHeaders.All;
                fwOptions.KnownIPNetworks.Clear();
                fwOptions.KnownProxies.Clear();
            });
        }

        if (simApiOptions.EnableLowerUrl)
        {
            builder.AddRouting(rOptions => rOptions.LowercaseUrls = true);
        }

        if (simApiOptions.EnableSimApiStorage)
        {
            builder.AddHttpContextAccessor();
            builder.AddSingleton<SimApiStorage>();
        }

        if (simApiOptions.EnableSimApiResponseFilter)
        {
            builder.AddControllers(opt => opt.Filters.Add<SimApiResponseFilter>())
                .AddJsonOptions(opt =>
                {
                    opt.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                });
        }

        if (simApiOptions.EnableSimApiAuthGate)
        {
            builder.AddSingleton<SimApiAuthClient>();
            builder.AddSingleton<SimApiAuthCenter>();
            if (simApiOptions.SimApiAuthCenterOptions.UseIam)
            {
                builder.AddSingleton<SimApiAuthIam>();
            }
        }

        builder.AddSingleton(simApiOptions.SimApiRequestLogOptions);
        return builder;
    }

    public static IHost UseSimApi(this IHost builder)
    {
        var options = builder.Services.GetRequiredService<SimApiOptions>();

        var logger = builder.Services.GetRequiredService<ILogger<SimApiOptions>>();

        logger.LogInformation("当前时区: {LocalId}", TimeZoneInfo.Local.Id);
        logger.LogInformation("主应用版本: {AppVersion}\nSimApi版本: {SimApiVersion}", SimApiUtil.AppVersion,
            SimApiUtil.SimApiVersion);

        if (options.RedisConfiguration != null)
        {
            logger.LogInformation("开始配置 RedisCache ...");
        }

        if (options.EnableSimApiCache)
        {
            logger.LogInformation("开始配置SimApiCache...");
        }

        //请求一下检测存储错误
        if (options.EnableSimApiStorage)
        {
            logger.LogInformation("开始配置SimApiStorage...");
            builder.Services.GetService<SimApiStorage>();
        }

        if (options.EnableSimApiHttpClient)
        {
            logger.LogInformation("开始配置SimApiHttpClient...\n服务器地址: {ApiUrl}\nAppId:{AuthUrl}n\nAppkey: {AppId}",
                options.SimApiHttpClientOptions.Server, options.SimApiHttpClientOptions.AppId,
                !string.IsNullOrEmpty(options.SimApiHttpClientOptions.AppKey));
        }

        if (options.EnableSynapse)
        {
            var synapse = builder.Services.GetRequiredService<Synapse>();
            synapse.Init();
        }

        if (options.EnableJob)
        {
            logger.LogInformation("开始配置 SimApiJob ...");
        }

        return builder;
    }

    /// <summary>
    ///  使用所有SimApi自定义中间件
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static WebApplication UseSimApi(this WebApplication builder)
    {
        var options = builder.Services.GetRequiredService<SimApiOptions>();
        var logger = builder.Services.GetRequiredService<ILogger<SimApiOptions>>();
        UseSimApi((IHost)builder);
        if (options.EnableForwardHeaders)
        {
            logger.LogInformation("开始配置ForwardedHeaders...");
            builder.UseForwardedHeaders();
        }

        if (options.EnableCors)
        {
            logger.LogInformation("开始配置Cors全部允许...");
            builder.UseCors("any");
        }

        if (options.EnableSimApiResponseFilter)
        {
            logger.LogInformation("开始配置SimApiResponseFilter...");
            builder.MapControllers();
        }

        var checkers = builder.Services.CreateScope().ServiceProvider.GetServices<ISimApiAuthChecker>().ToArray();
        if (checkers.Length != 0)
        {
            var msg = checkers.Aggregate("开始配置SimApiAuthChecker...",
                (current, checker) => current + $"\n|- {checker.GetType().FullName}");
            logger.LogInformation(msg);
        }

        if (options.EnableSimApiAuthGate)
        {
            logger.LogInformation("开始配置SimApiAuthGate...");
            if (string.IsNullOrEmpty(options.SimApiAuthCenterOptions.AppId) ||
                string.IsNullOrEmpty(options.SimApiAuthCenterOptions.AppKey))
            {
                logger.LogCritical("必须配置AuthGate的AppId和AppKey才能启用SimApiAuthGate");
            }
            else
            {
                if (options.SimApiAuthCenterOptions.UseMiddleware)
                {
                    builder.UseMiddleware<SimApiAuthCenterMiddleware>();
                }
            }
        }

        if (options.EnableSimApiAuth)
        {
            logger.LogInformation("开始配置SimApiAuth...");
            builder.UseMiddleware<SimApiAuthMiddleware>();
        }

        // 注册内置Route(UserInfo/Logout/WebConfig)。
        // 路径统一由 SimApiBuiltInRoutes 路由表提供: 配置为 null/空 的端点不注册。
        foreach (var route in SimApiBuiltInRoutes.Get(options.SimApiRouteOptions))
        {
            logger.LogInformation("注册内置Route: {RouteName} => {RoutePath} [{HttpMethods}]", route.RouteName,
                route.Path, string.Join(", ", route.HttpMethods));
            builder.MapControllerRoute(
                name: route.RouteName,
                pattern: route.Path,
                defaults: new
                {
                    controller = route.Controller,
                    action = route.Action
                });
        }

        if (options.EnableSimApiDoc)
        {
            logger.LogInformation("开始配置SimApiDoc...");
            var docOptions = options.SimApiDocOptions;
            builder.UseSwagger(x => x.RouteTemplate = $"/{docOptions.UrlPrefix}/{{documentName}}.json")
                .UseSwaggerUI(x =>
                {
                    x.RoutePrefix = docOptions.UrlPrefix;
                    x.DocumentTitle = docOptions.DocumentTitle;
                    foreach (var group in docOptions.ApiGroups)
                    {
                        // 相对端点: 页面位于 /{UrlPrefix}/, 自动解析为 /{UrlPrefix}/{group.Id}.json
                        x.SwaggerEndpoint($"{group.Id}.json", name: group.Name);
                    }

                    x.SupportedSubmitMethods(docOptions.SupportedMethod);
                    x.DisplayRequestDuration();
                });
        }

        if (options.EnableRequestLog)
        {
            logger.LogInformation("开始配置SimApiRequestLog...");
            builder.UseMiddleware<SimApiRequestLogMiddleware>();
        }

        if (options.EnableSimApiException)
        {
            logger.LogInformation("开始配置SimApiException...");
            builder.UseMiddleware<SimApiExceptionMiddleware>();
        }

        if (options.EnableLowerUrl)
        {
            logger.LogInformation("开始配置使用URL小写...");
        }

        if (options is { EnableJob: true, SimApiJobOptions.DashboardUrl: not null })
        {
            logger.LogInformation("开始配置 SimApiJob Web控制台...");
            builder.UseHangfireDashboard(options.SimApiJobOptions.DashboardUrl, new DashboardOptions
            {
                Authorization =
                [
                    new SimApiJobWebAuth(options.SimApiJobOptions.DashboardAuthUser,
                        options.SimApiJobOptions.DashboardAuthPass)
                ]
            });
        }

        return builder;
    }
}