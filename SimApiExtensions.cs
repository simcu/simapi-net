using System;
using SimApi.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using SimApi.Middlewares;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Logging;
using SimApi.Configs;
using SimApi.Logger;

namespace SimApi
{
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

            // 是否使用SIMAUTH
            if (simApiOptions.EnableSimApiAuth)
            {
                builder.AddScoped<SimApiAuth>();
            }

            if (simApiOptions.EnableCors)
            {
                builder.AddCors(cors => cors.AddPolicy("any",
                    policy => { policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin(); }));
            }

            // 使用SimApiDoc
            if (simApiOptions.EnableSimApiDoc)
            {
                var docOptions = simApiOptions.SimApiDocOptions;
                builder.AddSwaggerGen(x =>
                {
                    foreach (var group in docOptions.ApiGroups)
                    {
                        x.SwaggerDoc(group.Id, new OpenApiInfo {Title = group.Name, Description = group.Description});
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
                                                {Type = ReferenceType.SecurityScheme, Id = "HeaderToken"}
                                        },
                                        new[] {"readAccess", "writeAccess"}
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
                                        {Type = ReferenceType.SecurityScheme, Id = "oauth2"}
                                },
                                new[] {"SimApiAuth"}
                            }
                        });
                    }

                    if (haveSimApiAuth)
                    {
                        x.AddSecurityDefinition("HeaderToken",
                            new OpenApiSecurityScheme {Name = "Token", In = ParameterLocation.Header});
                    }
                });
            }

            // 使用Header转发，应对代理后获取真实ip
            if (simApiOptions.EnableForwardHeaders)
            {
                builder.Configure<ForwardedHeadersOptions>(options =>
                {
                    options.ForwardedHeaders = ForwardedHeaders.All;
                    options.KnownNetworks.Clear();
                    options.KnownProxies.Clear();
                });
            }

            if (simApiOptions.EnableLowerUrl)
            {
                builder.AddRouting(options => options.LowercaseUrls = true);
            }

            if (simApiOptions.EnableSimApiStorage)
            {
                builder.AddHttpContextAccessor();
                builder.AddSingleton<SimApiStorage>();
            }

            builder.AddSingleton(simApiOptions);
            return builder;
        }

        /// <summary>
        ///  使用所有SimApi自定义中间件
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseSimApi(this IApplicationBuilder builder)
        {
            var options = builder.ApplicationServices.GetService<SimApiOptions>();
            if (options.EnableLogger)
            {
                builder.ApplicationServices.GetService<ILoggerFactory>().AddProvider(new SimApiLoggerProvider());
            }

            var logger = builder.ApplicationServices.GetService<ILogger<SimApiLogger>>();

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
                builder.ApplicationServices.GetService<SimApiStorage>();
            }

            if (options.EnableLowerUrl)
            {
                logger.LogInformation("开始配置使用URL小写...");
            }

            return builder;
        }
    }
}