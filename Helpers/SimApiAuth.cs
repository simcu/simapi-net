using System;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Distributed;
using SimApi.Communications;
using StackExchange.Redis;

namespace SimApi.Helpers;

/// <summary>
/// 认证助手
/// </summary>
public class SimApiAuth(IDistributedCache cache, IConnectionMultiplexer redis)
{
    private const string TokenCacheKey = "SimApi:Auth:Token:{token}";
    private const string TokenSetCacheKey = "SimApi:Auth:User:{userId}";
    private readonly IDatabase _redisDb = redis.GetDatabase();

    /// <summary>
    /// 登录信息
    /// </summary>
    /// <param name="loginItem"></param>
    /// <param name="expireTime"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public string Login(SimApiLoginItem loginItem, TimeSpan? expireTime = null, string? token = null)
    {
        expireTime ??= TimeSpan.FromDays(7);
        token ??= Guid.NewGuid().ToString();
        var cacheKey = TokenCacheKey.Replace("{token}", token);
        var setCacheKey = TokenSetCacheKey.Replace("{userId}", loginItem.Id);
        _redisDb.SetAdd(setCacheKey, token);
        cache.SetString(cacheKey, SimApiUtil.Json(loginItem),
            new DistributedCacheEntryOptions
            {
                SlidingExpiration = expireTime
            });
        _redisDb.KeyExpire(setCacheKey, expireTime.Value);

        return token;
    }

    /// <summary>
    /// 更新登陆信息
    /// </summary>
    /// <param name="loginItem"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public string Update(SimApiLoginItem loginItem, string token)
    {
        var cacheKey = TokenCacheKey.Replace("{token}", token);
        cache.SetString(cacheKey, SimApiUtil.Json(loginItem));
        var ttl = _redisDb.KeyTimeToLive(cacheKey);
        var setCacheKey = TokenSetCacheKey.Replace("{userId}", loginItem.Id);
        _redisDb.KeyExpire(setCacheKey, ttl);
        return token;
    }


    /// <summary>
    /// 获取登陆信息
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public SimApiLoginItem? GetLogin(string token)
    {
        var cacheKey = TokenCacheKey.Replace("{token}", token);
        var login = cache.GetString(cacheKey);
        var resp = login != null ? SimApiUtil.FromJson<SimApiLoginItem>(login) : null;
        if (resp != null)
        {
            var ttl = _redisDb.KeyTimeToLive(cacheKey);
            var setCacheKey = TokenSetCacheKey.Replace("{userId}", resp.Id);
            _redisDb.KeyExpire(setCacheKey, ttl);
        }

        return resp;
    }

    /// <summary>
    /// 获取所有的登录token
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public SimApiLoginItem[] GetAllLogins(string userId)
    {
        var setCacheKey = TokenSetCacheKey.Replace("{userId}", userId);
        var allLogins = _redisDb.SetMembers(setCacheKey).ToStringArray();
        var resp = new List<SimApiLoginItem>();
        foreach (var login in allLogins)
        {
            if (login != null)
            {
                var item = GetLogin(login);
                if (item != null)
                {
                    resp.Add(item);
                }
                else
                {
                    _redisDb.SetRemove(setCacheKey, login);
                }
            }
        }

        return resp.ToArray();
    }

    /// <summary>
    /// 退出所有登录
    /// </summary>
    /// <param name="userId"></param>
    public void LogoutAll(string userId)
    {
        var setCacheKey = TokenSetCacheKey.Replace("{userId}", userId);
        var allLogins = _redisDb.SetMembers(setCacheKey).ToStringArray();
        foreach (var login in allLogins)
        {
            if (login != null)
            {
                var cacheKey = TokenCacheKey.Replace("{token}", login);
                cache.Remove(cacheKey);
            }
        }

        _redisDb.KeyDelete(setCacheKey);
    }

    /// <summary>
    /// 退出登陆
    /// </summary>
    /// <param name="token">登陆标识</param>
    public void Logout(string token)
    {
        var item = GetLogin(token);
        if (item != null)
        {
            var setCacheKey = TokenSetCacheKey.Replace("{userId}", item.Id);
            _redisDb.SetRemove(setCacheKey, token);
        }

        var cacheKey = TokenCacheKey.Replace("{token}", token);
        cache.Remove(cacheKey);
    }
}