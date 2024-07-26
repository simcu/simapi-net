using System;
using SimApi.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using SimApi.Middlewares;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        Action<SimApiOptions> options = null)
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

        if (simApiOptions.EnableCors)
        {
            builder.AddCors(cors => cors.AddPolicy("any",
                policy => { policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin(); }));
        }

        if (simApiOptions.EnableSynapse)
        {
            builder.AddSingleton<Synapse>();
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
                            new[] { "SimApiAuth" }
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