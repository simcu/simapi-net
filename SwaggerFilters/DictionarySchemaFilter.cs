using System.Collections.Generic;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SimApi.SwaggerFilters;

public class DictionarySchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (!context.Type.IsGenericType || context.Type.GetGenericTypeDefinition() != typeof(Dictionary<,>))
            return;

        if (schema is not OpenApiSchema concrete) return;

        var valueType = context.Type.GetGenericArguments()[1];
        concrete.Type = JsonSchemaType.Object;
        concrete.AdditionalPropertiesAllowed = true;
        concrete.AdditionalProperties = context.SchemaGenerator.GenerateSchema(valueType, context.SchemaRepository);
        concrete.Properties?.Clear();
        concrete.Example = null;
        concrete.Examples?.Clear();
    }
}