using System.Collections.Generic;
using System.Linq;
using SimApi.Attributes;

namespace SimApi.SwaggerFilters;

using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

/// <summary>
/// 自定义 Swagger 过滤器：将标注 [AesBody] 的参数显示在 Request Body 中
/// </summary>
public class AesBodyOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        foreach (var parameter in context.ApiDescription.ParameterDescriptions)
        {
            var hasAesBodyAttr = parameter.ParameterInfo()
                .GetCustomAttribute<AesBodyAttribute>() != null;
            if (!hasAesBodyAttr) continue;
            // 1. 移除默认的 Query 参数描述（如果存在）
            var queryParam = operation.Parameters
                .FirstOrDefault(p => p.Name == parameter.Name);
            if (queryParam != null)
            {
                operation.Parameters.Remove(queryParam);
            }

            // 2. 添加 Body 参数描述
            // 获取参数类型的 Schema（Swagger 模型定义）
            var schema = context.SchemaGenerator.GenerateSchema(
                parameter.Type,
                context.SchemaRepository);

            // 将参数添加到 Request Body
            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    {
                        "application/json", // 假设使用 JSON 格式
                        new OpenApiMediaType { Schema = schema }
                    }
                },
                Description = "内容为加密前内容,需要转换为JSON后使用AES加密后提交,提交格式为 {\"data\":\"AES密文\"}",
                Required = true // 标记为必填
            };
        }
    }
}