using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SimApi.Attributes;
using SimApi.Communications;

namespace SimApi.Helpers;

public class WrapResponseSchemaFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // 1. 跳过标记了 [OriginResponseAttribute] 的接口（不包装）
        if (context.MethodInfo.DeclaringType?.GetCustomAttributes<OriginResponseAttribute>(true).Any() == true
            || context.MethodInfo.GetCustomAttributes<OriginResponseAttribute>(true).Any())
        {
            return;
        }

        // 2. 获取控制器方法声明的“原始返回类型”（如 Task<UserDto> → UserDto）
        var returnType = GetUnwrappedReturnType(context.MethodInfo.ReturnType, context);
        if (IsAlreadyWrappedType(returnType))
        {
            return; // 保留原始返回类型的文档描述
        }

        // 3. 定义包装后的目标类型（泛型/非泛型）
        Type wrappedType;
        if (returnType == typeof(void) || returnType == typeof(EmptyResult))
        {
            // 无数据：用非泛型 SimApiBaseResponse
            wrappedType = typeof(SimApiBaseResponse);
        }
        else
        {
            // 有数据：用泛型 SimApiBaseResponse<T>（T 为原始返回类型）
            wrappedType = typeof(SimApiBaseResponse<>).MakeGenericType(returnType);
        }

        // 4. 让 Swagger 生成包装类型的 Schema
        var schema = context.SchemaGenerator.GenerateSchema(wrappedType, context.SchemaRepository);

        // 5. 替换 Swagger 文档中的响应类型（只保留 200 OK 的响应，匹配过滤器逻辑）
        operation.Responses.Clear(); // 清除默认响应（如 200 返回原始类型）
        operation.Responses.Add("200", new OpenApiResponse
        {
            Description = "请求成功",
            Content = new Dictionary<string, OpenApiMediaType>
            {
                {
                    "application/json", // 只保留 JSON 格式（配合之前的全局配置）
                    new OpenApiMediaType
                    {
                        Schema = schema
                    }
                }
            }
        });
    }

    private static Type GetUnwrappedReturnType(Type returnType, OperationFilterContext context)
    {
        // 处理 Task<T>（异步方法）
        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            returnType = returnType.GetGenericArguments()[0];
        }

        // 处理 IActionResult/ObjectResult（如 return Ok(userDto)）
        if (!typeof(IActionResult).IsAssignableFrom(returnType)) return returnType;
        // 从方法返回语句中提取原始类型（需结合特性，或默认用 object）
        // 更精准的方式：让控制器方法用 [ProducesResponseType(typeof(UserDto), 200)] 声明原始类型
        var producesAttr = context.MethodInfo.GetCustomAttributes<ProducesResponseTypeAttribute>()
            .FirstOrDefault(a => a.StatusCode == 200);
        if (producesAttr?.Type != null && producesAttr.Type != typeof(void))
        {
            return producesAttr.Type;
        }

        return typeof(object); // 无法识别时默认用 object
    }

    // 辅助方法：判断类型是否已经是 SimApiBaseResponse 及其泛型
    private static bool IsAlreadyWrappedType(Type type)
    {
        // 情况1：类型本身是 SimApiBaseResponse 或其泛型
        if (type == typeof(SimApiBaseResponse) ||
            (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(SimApiBaseResponse<>)))
        {
            return true;
        }

        // 情况2：类型是 SimApiBaseResponse 的子类（非泛型）
        if (type.IsSubclassOf(typeof(SimApiBaseResponse)))
        {
            return true;
        }

        // 情况3：类型是 SimApiBaseResponse<T> 的子类（泛型）
        // 检查是否继承自泛型父类 SimApiBaseResponse<>（任意 T）
        if (type.BaseType is { IsGenericType: true })
        {
            var genericBaseType = type.BaseType.GetGenericTypeDefinition();
            if (genericBaseType == typeof(SimApiBaseResponse<>))
            {
                return true;
            }
        }

        // 其他情况：不是包装类型或其子类
        return false;
    }
}