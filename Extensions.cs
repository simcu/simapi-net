﻿using System;
using YYApi.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;
using YYApi.Middlewares;

namespace YYApi
{
    /// <summary>
    /// 加入系统的扩展信息
    /// </summary>
    public static class Extensions
    {
        //**********快捷添加**************

        /// <summary>
        /// 添加整个YYAPI,同时增加CORS规则
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="title"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public static IServiceCollection AddYYApi(this IServiceCollection builder, string title,
            string description = null)
        {
            return builder.AddYYAuth().AddYYDoc(title, description).AddCors().AddYYUpload();
        }

        /// <summary>
        ///  使用所有YYApi自定义中间件
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="title"></param>
        /// <param name="staticFileroot"></param>
        /// <param name="submitMethods"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseYYApi(this IApplicationBuilder builder, string title = "API文档", params SubmitMethod[] submitMethods)
        {
            return builder.UseYYException().UseYYDoc(title, submitMethods).UseMiddleware<YYAuthMiddleware>().UseCors(x => x.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()).UseYYUpload();
        }



        //*********组件快捷方式*************

        /// <summary>
        /// 添加自定义授权认证中间价
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IServiceCollection AddYYAuth(this IServiceCollection builder)
        {
            return builder.AddScoped<YYAuth>();
        }

        /// <summary>
        /// 添加Base64上传组件
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IServiceCollection AddYYUpload(this IServiceCollection builder)
        {
            return builder.AddSingleton<YYUpload>();
        }

        /// <summary>
        /// 添加Api文档服务
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="title">文档标题</param>
        /// <param name="description">文档描述</param>
        /// <returns></returns>
        public static IServiceCollection AddYYDoc(this IServiceCollection builder, string title,
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
        public static IApplicationBuilder UseYYException(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<YYExceptionMiddleware>();
        }

        /// <summary>
        /// 配置上传组件必须
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseYYUpload(this IApplicationBuilder builder)
        {
            return builder.UseStaticFiles();
        }

        /// <summary>
        /// 使用文档UI
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="title">文档标题</param>
        /// <param name="submitMethods">文档支持的提交方式(如果不指定,默认使用POST)</param>
        /// <returns></returns>
        public static IApplicationBuilder UseYYDoc(this IApplicationBuilder builder, string title,
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
        public static IApplicationBuilder UseYYDocEx(this IApplicationBuilder builder, string title = "API文档",
            params SubmitMethod[] submitMethods)
        {
            return builder.UseYYException().UseYYDoc(title, submitMethods);
        }

        /// <summary>
        /// 使用自定义认证中间件（依赖IDistributedCache）
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseYYAuth(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<YYAuthMiddleware>();
        }


    }
}