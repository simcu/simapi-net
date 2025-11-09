using System;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.OpenApi.Any;

namespace SimApi.SwaggerFilters;

public class GlobalDynamicObjectSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (!IsDynamicObjectType(context.Type)) return;
        schema.AdditionalPropertiesAllowed = true;
        schema.AdditionalProperties = new OpenApiSchema
        {
            Type = "object", // 表示 value 可以是任意类型（兼容所有类型）
            Nullable = true
        };

        // 2. 覆盖默认示例，使用包含多种类型的示例
        schema.Example = CreateMultiTypeExample();
    }

    // 判断是否为需要处理的“动态对象”类型
    private bool IsDynamicObjectType(Type? type)
    {
        if (type == null) return false;
        if (typeof(IDictionary).IsAssignableFrom(type) ||
            (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>)))
        {
            return true;
        }
        if (type == typeof(object))
        {
            return true;
        }
        return type.Name.Contains("AnonymousType") && type.Namespace == null;
    }

    // 创建包含多种类型的示例（覆盖默认的 string 示例）
    private OpenApiObject CreateMultiTypeExample()
    {
        return new OpenApiObject
        {
            ["stringProp"] = new OpenApiString("example string"), // 字符串
            ["numberProp"] = new OpenApiInteger(123), // 数字
            ["boolProp"] = new OpenApiBoolean(true), // 布尔值
            ["objectProp"] = new OpenApiObject // 嵌套对象
            {
                ["nestedKey"] = new OpenApiString("nested value")
            },
            ["arrayProp"] = new OpenApiArray // 数组
            {
                new OpenApiInteger(1),
                new OpenApiString("two")
            }
        };
    }
}