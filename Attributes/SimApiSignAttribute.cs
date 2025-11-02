using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using SimApi.Exceptions;
using SimApi.Helpers;
using SimApi.ModelBinders;

namespace SimApi.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SimApiSignAttribute : ActionFilterAttribute
{
    public Type KeyProvider { get; set; } = typeof(SimApiSignProviderBase);

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.HttpContext.RequestServices.GetService(KeyProvider) is not SimApiSignProviderBase keyProvider)
        {
            throw new SimApiException(400, "未配置签名器");
        }

        string? appId = null;
        if (!string.IsNullOrEmpty(keyProvider.AppIdName))
        {
            appId = context.HttpContext.Request.Query[keyProvider.AppIdName]
                .FirstOrDefault() ?? context.HttpContext.Request.Headers[keyProvider.AppIdName]
                .FirstOrDefault();

            if (string.IsNullOrEmpty(appId))
            {
                throw new SimApiException(400, $"获取{keyProvider.AppIdName}失败");
            }
        }

        // 4. 通过接口获取密钥（解耦的核心：不再直接依赖数据库）
        var key = keyProvider.GetKey(appId);
        if (string.IsNullOrEmpty(key))
        {
            throw new SimApiException(400, "获取签名KEY失败");
        }

        var timestamp = context.HttpContext.Request.Query[keyProvider.TimestampName]
            .FirstOrDefault() ?? context.HttpContext.Request.Headers[keyProvider.TimestampName]
            .FirstOrDefault();
        if (string.IsNullOrEmpty(timestamp))
        {
            throw new SimApiException(400, $"{keyProvider.TimestampName}不能为空");
        }

        var nonce = context.HttpContext.Request.Query[keyProvider.NonceName]
            .FirstOrDefault() ?? context.HttpContext.Request.Headers[keyProvider.NonceName]
            .FirstOrDefault();
        if (string.IsNullOrEmpty(nonce))
        {
            throw new SimApiException(400, $"{keyProvider.NonceName}不能为空");
        }

        if (!int.TryParse(timestamp, out var ts))
        {
            throw new SimApiException(400, $"{keyProvider.TimestampName}格式错误");
        }

        if (keyProvider.QueryExpires != 0)
        {
            if (ts > SimApiUtil.TimestampNow + 2)
            {
                throw new SimApiException(400, "请校准本地时间");
            }

            if (ts + keyProvider.QueryExpires < SimApiUtil.TimestampNow)
            {
                throw new SimApiException(400, "请求已过期");
            }

            if (keyProvider.DuplicateRequestProtection)
            {
                var cache = context.HttpContext.RequestServices.GetRequiredService<IDistributedCache>();
                if (cache.GetString("SignQuery:" + nonce) == null)
                {
                    cache.SetString("SignQuery:" + nonce, timestamp, new DistributedCacheEntryOptions()
                    {
                        SlidingExpiration = TimeSpan.FromSeconds(keyProvider.QueryExpires + 2)
                    });
                }
                else
                {
                    throw new SimApiException(400, "重复请求");
                }
            }
        }

        var signStr = string.Empty;
        foreach (var item in keyProvider.SignFields)
        {
            signStr += $"{item}=";
            signStr += context.HttpContext.Request.Query[item]
                .FirstOrDefault() ?? context.HttpContext.Request.Headers[item]
                .FirstOrDefault();
            signStr += "&";
        }

        if (!string.IsNullOrEmpty(keyProvider.AppIdName))
        {
            signStr += $"{keyProvider.AppIdName}={appId}&";
        }

        signStr += $"{keyProvider.TimestampName}={ts}&{keyProvider.NonceName}={nonce}&{key}";
        var sign = context.HttpContext.Request.Query[keyProvider.SignName]
            .FirstOrDefault() ?? context.HttpContext.Request.Headers[keyProvider.SignName]
            .FirstOrDefault();
        if (SimApiUtil.Md5(signStr) != sign)
        {
            throw new SimApiException(400, "签名错误");
        }
    }
}