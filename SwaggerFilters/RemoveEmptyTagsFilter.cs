using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

namespace SimApi.SwaggerFilters;

public class RemoveEmptyTagsFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        // 步骤1：收集所有有接口的 Tag 名称
        var tagsWithOperations = swaggerDoc.Paths.Values
            .SelectMany(path => path.Operations.Values)
            .SelectMany(op => op.Tags.Select(t => t.Name))
            .Distinct()
            .ToList();

        // 步骤2：移除无接口的空 Tag
        var emptyTags = swaggerDoc.Tags.Where(t => !tagsWithOperations.Contains(t.Name)).ToList();
        foreach (var tag in emptyTags)
        {
            swaggerDoc.Tags.Remove(tag);
        }
    }
}