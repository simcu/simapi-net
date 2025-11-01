using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using SimApi.Attributes;

namespace SimApi.Helpers
{
    public class SimApiAuthOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // 检查接口或控制器是否标记了 [SimApiAuth] 特性
            var requiresAuth =
                // 方法上有 [SimApiAuth]
                context.MethodInfo.GetCustomAttributes<SimApiAuthAttribute>(true).Any()
                ||
                // 控制器上有 [SimApiAuth]（继承到所有方法）
                context.MethodInfo.DeclaringType?.GetCustomAttributes<SimApiAuthAttribute>(true).Any() == true;
            if (requiresAuth)
            {
                // 添加授权要求：关联步骤 2 中定义的 "SimApiAuth" 安全方案
                operation.Security = new List<OpenApiSecurityRequirement>
                {
                    new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "SimApiAuth" // 必须与 AddSecurityDefinition 的第一个参数一致
                                }
                            },
                            [] // 无需指定作用域（scope）时留空
                        }
                    }
                };
            }
            // 未标记 [SimApiAuth] 的接口：不添加安全要求，Swagger 不显示锁图标
        }
    }
}