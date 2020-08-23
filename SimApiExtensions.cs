using System;
using SimApi.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;
using SimApi.Middlewares;

namespace SimApi
{
    /// <summary>
    /// 加入系统的扩展信息
    /// </summary>
    public static class Extensions
    {
        public static string DocumentTitle = "";
        public static string DocumentDescription = "";

        //**********快捷添加**************

        /// <summary>
        /// 添加整个SimAPI,同时增加CORS规则
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="title"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public static IServiceCollection AddSimApi(this IServiceCollection builder, string title,
            string description = null)
        {
            return builder.AddSimApiAuth().AddSimApiDoc(title, description).AddCors().AddSimApiUpload();
        }

        /// <summary>
        ///  使用所有SimApi自定义中间件
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="title"></param>
        /// <param name="staticFileroot"></param>
        /// <param name="submitMethods"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseSimApi(this IApplicationBuilder builder, params SubmitMethod[] submitMethods)
        {
            return builder.UseSimApiException().UseSimApiDoc(submitMethods).UseMiddleware<SimApiAuthMiddleware>().UseCors(x => x.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()).UseSimApiUpload();
        }



        //*********组件快捷方式*************

        /// <summary>
        /// 添加自定义授权认证中间价
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IServiceCollection AddSimApiAuth(this IServiceCollection builder)
        {
            return builder.AddScoped<SimApiAuth>();
        }

        /// <summary>
        /// 添加Base64上传组件
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IServiceCollection AddSimApiUpload(this IServiceCollection builder)
        {
            return builder.AddSingleton<SimApiUpload>();
        }

        /// <summary>
        /// 添加Api文档服务
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="title">文档标题</param>
        /// <param name="description">文档描述</param>
        /// <returns></returns>
        public static IServiceCollection AddSimApiDoc(this IServiceCollection builder, string title,
            string description = null)
        {
            DocumentTitle = title;
            DocumentDescription = description;
            return builder.AddSwaggerGen(x =>
            {
                x.SwaggerDoc("api", new OpenApiInfo { Title = DocumentTitle, Description = DocumentDescription });
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
        public static IApplicationBuilder UseSimApiException(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SimApiExceptionMiddleware>();
        }

        /// <summary>
        /// 配置上传组件必须
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseSimApiUpload(this IApplicationBuilder builder)
        {
            return builder.UseStaticFiles(new StaticFileOptions
            {
                DefaultContentType = "application/x-msdownload",
                ServeUnknownFileTypes = true
            });
        }

        /// <summary>
        /// 使用文档UI
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="title">文档标题</param>
        /// <param name="submitMethods">文档支持的提交方式(如果不指定,默认使用POST)</param>
        /// <returns></returns>
        public static IApplicationBuilder UseSimApiDoc(this IApplicationBuilder builder, params SubmitMethod[] submitMethods)
        {
            return builder.UseSwagger(x => x.RouteTemplate = "docs/{documentName}.json").UseSwaggerUI(x =>
            {
                x.RoutePrefix = "docs";
                x.DocumentTitle = DocumentTitle;
                x.SwaggerEndpoint("/docs/api.json", name: DocumentTitle);
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
        public static IApplicationBuilder UseSimApiDocEx(this IApplicationBuilder builder, params SubmitMethod[] submitMethods)
        {
            return builder.UseSimApiException().UseSimApiDoc(submitMethods);
        }

        /// <summary>
        /// 使用自定义认证中间件（依赖IDistributedCache）
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseSimApiAuth(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SimApiAuthMiddleware>();
        }


    }
}