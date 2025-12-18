using System;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SimApi.SwaggerFilters;

public class GlobalDynamicObjectSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (!IsDynamicObjectType(context.Type)) return;
        var oaSchema = schema as OpenApiSchema;
        oaSchema.AdditionalPropertiesAllowed = true;
        oaSchema.AdditionalProperties = new OpenApiSchema
        {
            Type = JsonSchemaType.Object, // 表示 value 可以是任意类型（兼容所有类型）
        };

        // 2. 覆盖默认示例，使用包含多种类型的示例
        oaSchema.Example = CreateMultiTypeExample();
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
    private JsonNode CreateMultiTypeExample()
    {
        return new JsonObject
        {
            // 字符串类型：JsonValue.Create 包装字符串
            ["stringProp"] = JsonValue.Create("example string"),
            // 数字类型：支持 int/long/double 等，JsonValue 自动适配
            ["numberProp"] = JsonValue.Create(123),
            // 布尔类型
            ["boolProp"] = JsonValue.Create(true),
            // 嵌套对象：JsonObject 对应 OpenApiObject
            ["objectProp"] = new JsonObject
            {
                ["nestedKey"] = JsonValue.Create("nested value")
            },
            // 数组类型：JsonArray 对应 OpenApiArray
            ["arrayProp"] = new JsonArray
            {
                JsonValue.Create(1), // 数组内数字
                JsonValue.Create("two") // 数组内字符串
            },
            // 可选：添加 null 值示例（若需要）
            ["nullProp"] = null
        };
    }
}