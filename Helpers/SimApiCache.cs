using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace SimApi.Helpers;

public class SimApiCache(IDistributedCache cache)
{
    private const string Prefix = "SimApi:Cache:";

    /// <summary>
    /// 设置缓存 (值不能为null)
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="options"></param>
    public void Set(string key, object value, DistributedCacheEntryOptions? options = null)
    {
        SimApiError.ErrorWhenNull(value, 400, "缓存值不能为null");
        if (options is not null)
        {
            cache.SetString(Prefix + key, SimApiUtil.Json(value), options);
        }
        else
        {
            cache.SetString(Prefix + key, SimApiUtil.Json(value));
        }
    }

    /// <summary>
    /// 移除缓存
    /// </summary>
    /// <param name="key"></param>
    public void Remove(string key)
    {
        cache.Remove(Prefix + key);
    }

    /// <summary>
    /// 缓存Key是否存在
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public bool HasKey(string key)
    {
        return Get<string>(key) != null;
    }

    /// <summary>
    /// 获取string类型缓存
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public string? Get(string key)
    {
        return Get<string>(Prefix + key);
    }

    /// <summary>
    /// 获取特定类型缓存
    /// </summary>
    /// <param name="key"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T? Get<T>(string key)
    {
        var data = cache.GetString(Prefix + key);
        return data == null ? default : SimApiUtil.FromJson<T>(data);
    }
}