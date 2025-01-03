﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using SimApi.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using SimApi.Middlewares;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SimApi.Attributes;
using SimApi.CoceSdk;
using SimApi.Configurations;
using SimApi.Logger;

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

        if (simApiOptions.EnableCoceSdk)
        {
            builder.AddSingleton<CoceApp>();
        }

        if (simApiOptions.EnableCors)
        {
            builder.AddCors(cors => cors.AddPolicy("any",
                policy => { policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin(); }));
        }

        if (simApiOptions.EnableSynapse)
        {
            builder.AddSingleton<Synapse>();
            //自动依赖注入
            var stackTrace = new StackTrace();
            var callingMethod = stackTrace.GetFrame(stackTrace.FrameCount - 1)?.GetMethod();
            var assembly = callingMethod?.DeclaringType?.Assembly;
            var types = assembly?.GetTypes() ?? [];
            foreach (var type in types)
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

                x.EnableAnnotations();
                var haveOauth = false;
                var haveSimApiAuth = false;
                var oauthFlows = new OpenApiOAuthFlows();
                foreach (var auth in docOptions.ApiAuth.Type)
                {
                    switch (auth)
                    {
                        case "SimApiAuth":
                            x.AddSecurityRequirement(new OpenApiSecurityRequirement
                            {
                                {
                                    new OpenApiSecurityScheme
                                    {
                                        Reference = new OpenApiReference
                                        {
                                            Type = ReferenceType.SecurityScheme,
                                            Id = "HeaderToken"
                                        }
                                    },
                                    new[] { "readAccess", "writeAccess" }
                                }
                            });
                            haveSimApiAuth = true;
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
                    x.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "oauth2"
                                }
                            },
                            ["SimApiAuth"]
                        }
                    });
                }

                if (haveSimApiAuth)
                {
                    x.AddSecurityDefinition("HeaderToken",
                        new OpenApiSecurityScheme
                        {
                            Name = "Token",
                            In = ParameterLocation.Header
                        });
                }
            });
        }

        // 使用Header转发，应对代理后获取真实ip
        if (simApiOptions.EnableForwardHeaders)
        {
            builder.Configure<ForwardedHeadersOptions>(fwOptions =>
            {
                fwOptions.ForwardedHeaders = ForwardedHeaders.All;
                fwOptions.KnownNetworks.Clear();
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
                .AddJsonOptions(opt => opt.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);
        }

        builder.AddSingleton(simApiOptions);
        return builder;
    }

    public static IHost UseSimApi(this IHost builder)
    {
        var options = builder.Services.GetRequiredService<SimApiOptions>();

        var logger = builder.Services.GetRequiredService<ILogger<SimApiOptions>>();

        logger.LogInformation("当前时区: {LocalId}", TimeZoneInfo.Local.Id);

        //请求一下检测存储错误
        if (options.EnableSimApiStorage)
        {
            logger.LogInformation("开始配置SimApiStorage...");
            builder.Services.GetService<SimApiStorage>();
        }

        if (options.EnableSimApiResponseFilter)
        {
            logger.LogInformation("开始配置SimApiResponseFilter...");
        }

        if (options.EnableCoceSdk)
        {
            logger.LogInformation("开始配置CoceAppSdk...\nApi入口: {ApiUrl}\nAuth入口:{AuthUrl}n\nAppId: {AppId}",
                options.CoceSdkOptions.ApiEndpoint, options.CoceSdkOptions.AuthEndpoint,
                options.CoceSdkOptions.AppId);
        }

        if (options.EnableLowerUrl)
        {
            logger.LogInformation("开始配置使用URL小写...");
        }

        if (options.EnableSynapse)
        {
            var synapse = builder.Services.GetRequiredService<Synapse>();
            synapse.Init();
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

        logger.LogInformation("当前时区: {LocalId}", TimeZoneInfo.Local.Id);
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

        if (options.EnableSimApiAuth)
        {
            logger.LogInformation("开始配置SimApiAuth...");
            builder.UseMiddleware<SimApiAuthMiddleware>();
            builder.MapControllerRoute(name: "GetUserInfo", pattern: "/user/info",
                defaults: new { controller = "SimApiCommon", action = "UserInfo" });
            builder.MapControllerRoute(name: "CheckLogin", pattern: "/auth/check",
                defaults: new { controller = "SimApiCommon", action = "CheckLogin" });
            builder.MapControllerRoute(name: "Logout", pattern: "/auth/logout",
                defaults: new { controller = "SimApiCommon", action = "Logout" });
            if (options.EnableCoceSdk)
            {
                logger.LogInformation("开始配置CoceAppSdk...\nApi入口: {ApiUrl}\nAuth入口:{AuthUrl}n\nAppId: {AppId}",
                    options.CoceSdkOptions.ApiEndpoint, options.CoceSdkOptions.AuthEndpoint,
                    options.CoceSdkOptions.AppId);
                builder.MapControllerRoute(name: "LoginUseCoce", pattern: "/auth/login",
                    defaults: new { controller = "SimApiCoce", action = "Login" });
                builder.MapControllerRoute(name: "LoginUseCoce", pattern: "/user/groups",
                    defaults: new { controller = "SimApiCoce", action = "ListGroups" });
                builder.MapControllerRoute(name: "LoginUseCoce", pattern: "/auth/config",
                    defaults: new { controller = "SimApiCoce", action = "GetConfig" });
            }
        }

        if (options.EnableSimApiDoc)
        {
            logger.LogInformation("开始配置SimApiDoc...");
            var docOptions = options.SimApiDocOptions;
            builder.UseSwagger(x => x.RouteTemplate = "/swagger/{documentName}.json").UseSwaggerUI(x =>
            {
                x.DocumentTitle = docOptions.DocumentTitle;
                foreach (var group in docOptions.ApiGroups)
                {
                    x.SwaggerEndpoint($"/swagger/{group.Id}.json", name: group.Name);
                }

                x.EnableValidator();
                x.SupportedSubmitMethods(docOptions.SupportedMethod);
                x.DisplayRequestDuration();
            });
        }

        if (options.EnableSimApiException)
        {
            logger.LogInformation("开始配置SimApiException...");
            builder.UseMiddleware<SimApiExceptionMiddleware>();
        }

        //请求一下检测存储错误
        if (options.EnableSimApiStorage)
        {
            logger.LogInformation("开始配置SimApiStorage...");
            builder.Services.GetService<SimApiStorage>();
        }

        if (options.EnableLowerUrl)
        {
            logger.LogInformation("开始配置使用URL小写...");
        }

        if (options.EnableSynapse)
        {
            var synapse = builder.Services.GetRequiredService<Synapse>();
            synapse.Init();
        }

        return builder;
    }
}