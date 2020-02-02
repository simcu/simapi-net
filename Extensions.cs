using System;
using YYApi.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace YYApi
{
    /// <summary>
    /// 加入系统的扩展信息
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// 添加自定义授权认证中间价
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IServiceCollection AddAuth(this IServiceCollection builder)
        {
            return builder.AddScoped<Auth>();
        }

        /// <summary>
        /// 添加整个YYAPI
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="title"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public static IServiceCollection AddYYApi(this IServiceCollection builder, string title,
            string description = null)
        {
            return builder.AddAuth().AddApiDoc(title, description).AddCors();
        }

        /// <summary>
        /// 添加Api文档服务
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="title">文档标题</param>
        /// <param name="description">文档描述</param>
        /// <returns></returns>
        public static IServiceCollection AddApiDoc(this IServiceCollection builder, string title,
            string description = null)
        {
            return builder.AddSwaggerGen(x =>
            {
                x.SwaggerDoc("api", new OpenApiInfo { Title = title, Description = description });
                x.EnableAnnotations();
                x.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference {Type = ReferenceType.SecurityScheme, Id = "HeaderToken"}
                        },
                        new[] {"readAccess", "writeAccess"}
                    }
                });
                x.AddSecurityDefinition("HeaderToken",
                    new OpenApiSecurityScheme { Name = "Token", In = ParameterLocation.Header });
            });
        }


        /// <summary>
        /// 使用异常中间价扩展
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseExceptionMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionMiddleware>();
        }

        /// <summary>
        /// 使用文档UI
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="title">文档标题</param>
        /// <param name="submitMethods">文档支持的提交方式(如果不指定,默认使用POST)</param>
        /// <returns></returns>
        public static IApplicationBuilder UseApiDoc(this IApplicationBuilder builder, string title,
            params SubmitMethod[] submitMethods)
        {
            return builder.UseSwagger(x => x.RouteTemplate = "docs/{documentName}.json").UseSwaggerUI(x =>
            {
                x.RoutePrefix = "docs";
                x.DocumentTitle = title;
                x.SwaggerEndpoint("/docs/api.json", name: title);
                x.EnableValidator();
                if (submitMethods.Length > 0)
                {
                    x.SupportedSubmitMethods(submitMethods);
                }
                else
                {
                    x.SupportedSubmitMethods(SubmitMethod.Post);
                }

                x.DisplayRequestDuration();
            });
        }


        /// <summary>
        /// 同时使用Api文档和异常处理中间件
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="title">文档标题</param>
        /// <param name="submitMethods">API提交方式定义</param>
        /// <returns></returns>
        public static IApplicationBuilder UseDocAndException(this IApplicationBuilder builder, string title = "API文档",
            params SubmitMethod[] submitMethods)
        {
            return builder.UseExceptionMiddleware().UseApiDoc(title, submitMethods);
        }

        /// <summary>
        /// 使用自定义认证中间件（依赖IDistributedCache）
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseAuthMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AuthMiddleware>();
        }

        /// <summary>
        /// 使用所有YYApi自定义中间件
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="title"></param>
        /// <param name="submitMethods"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseYYApiMiddleware(this IApplicationBuilder builder, string title = "API文档",
            params SubmitMethod[] submitMethods)
        {
            return builder.UseExceptionMiddleware().UseApiDoc(title, submitMethods).UseMiddleware<AuthMiddleware>().UseCors(x =>
            {
                x.AllowAnyHeader();
                x.AllowAnyMethod();
                x.AllowAnyOrigin();
            }); ;
        }
    }
}