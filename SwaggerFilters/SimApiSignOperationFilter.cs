using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using SimApi.Attributes;
using SimApi.ModelBinders;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SimApi.SwaggerFilters;

public class SimApiSignOperationFilter(IServiceProvider serviceProvider) : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // 1. 检查当前方法或类是否标注了 SimApiSignAttribute 及其子类
        var signAttribute = context.MethodInfo.GetCustomAttribute<SimApiSignAttribute>(inherit: true)
                            ?? context.MethodInfo.DeclaringType?.GetCustomAttribute<SimApiSignAttribute>(inherit: true);

        if (signAttribute == null)
        {
            return;
        }

        var keyProviderType = signAttribute.KeyProvider;
        if (!typeof(SimApiSignProviderBase).IsAssignableFrom(keyProviderType))
        {
            throw new InvalidOperationException($"KeyProvider 必须继承自 {nameof(SimApiSignProviderBase)}");
        }

        SimApiSignProviderBase? keyProvider;
        try
        {
            keyProvider =
                serviceProvider.CreateScope().ServiceProvider.GetService(keyProviderType) as SimApiSignProviderBase;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"无法从 DI 容器获取 {keyProviderType.Name} 实例：{ex.Message}");
        }

        if (keyProvider == null)
        {
            throw new InvalidOperationException($"{keyProviderType.Name} 未在 DI 容器中注册");
        }

        var signStr = keyProvider.SignFields.Aggregate(string.Empty, (current, field) => current + $"{field}=xxx&");
        if (!string.IsNullOrEmpty(keyProvider.AppIdName))
        {
            signStr += $"{keyProvider.AppIdName}=xxx&";
        }

        signStr += $"{keyProvider.TimestampName}=xxx&{keyProvider.NonceName}=xxx&签名密钥";

        var signParameters = new List<(string Name, string Description, bool Required)>
        {
            (keyProvider.TimestampName, "时间戳（秒级）", true),
            (keyProvider.NonceName, "随机字符串", true),
            (keyProvider.SignName, $"MD5签名结果,签名MD5字符串: {signStr}", true)
        };
        if (keyProvider.AppIdName != null)
        {
            signParameters.Add((keyProvider.AppIdName, "应用标识", true));
        }

        signParameters.AddRange(keyProvider.SignFields.Where(x => x != keyProvider.AppIdName)
            .Select(f => (f, string.Empty, true)));

        foreach (var (name, description, required) in signParameters)
        {
            if (operation.Parameters?.Any(p => p.Name == name) == true)
            {
                var tmp = operation.Parameters?.FirstOrDefault(p => p.Name == name);
                if (tmp != null)
                {
                    tmp.Required = required;
                    tmp.Description = description;
                }
                continue;
            }

            operation.Parameters ??= new List<OpenApiParameter>();
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = name,
                In = ParameterLocation.Query, // 指定为 Query 参数
                Description = description,
                Required = required,
                Schema = new OpenApiSchema
                {
                    Type = "string" // 签名相关参数通常为字符串类型
                }
            });
        }
    }
}