using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using SimApi.Attributes;

namespace SimApi.SwaggerFilters
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
            if (!requiresAuth) return;
            operation.Security ??= new List<OpenApiSecurityRequirement>();
            operation.Security.Add(new OpenApiSecurityRequirement()
            {
                { new OpenApiSecuritySchemeReference("SimApiAuth", context.Document), [] }
            });
        }
    }
}