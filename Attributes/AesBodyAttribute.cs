using System;
using Microsoft.AspNetCore.Mvc;
using SimApi.ModelBinders;

namespace SimApi.Attributes;

[AttributeUsage(AttributeTargets.Parameter)]
public class AesBodyAttribute : ModelBinderAttribute
{
    public Type KeyProvider { get; set; } = typeof(AesBodyProviderBase);

    public AesBodyAttribute()
    {
        // 指定使用自定义的模型绑定器
        BinderType = typeof(AesBodyModelBinder);
        Name = KeyProvider.FullName;
    }
}