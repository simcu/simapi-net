using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace SimApi.Helpers;

public class SimApiCache(IDistributedCache cache)
{
    private const string Prefix = "SimApi:Cache";

    public void Set(string key, object value, DistributedCacheEntryOptions? options = null)
    {
        if (options is not null)
        {
            cache.SetString(Prefix + key, SimApiUtil.Json(value), options);
        }
        else
        {
            cache.SetString(Prefix + key, SimApiUtil.Json(value));
        }
    }

    public string? Get(string key)
    {
        return cache.GetString(Prefix + key);
    }

    public T? Get<T>(string key)
    {
        var data = cache.GetString(Prefix + key);
        return data == null ? default : JsonSerializer.Deserialize<T>(data);
    }
}