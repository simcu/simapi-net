using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using SimApi.Communications;
using StackExchange.Redis;

namespace SimApi.Helpers;

/// <summary>
/// 认证助手（支持 Redis 和 InMemory 两种模式）
/// </summary>
public class SimApiAuth
{
    private const string TokenCacheKey = "SimApi:Auth:Token:{token}";
    private const string TokenSetCacheKey = "SimApi:Auth:User:{userId}";

    private readonly IDistributedCache _cache;
    private readonly IDatabase? _redisDb;

    // InMemory 模式：用户 → Token集合
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _userTokens = new();

    public SimApiAuth(IDistributedCache cache, IServiceProvider sp)
    {
        _cache = cache;
        _redisDb = sp.GetService<IConnectionMultiplexer>()?.GetDatabase();
    }

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

        _cache.SetString(cacheKey, SimApiUtil.Json(loginItem),
            new DistributedCacheEntryOptions { SlidingExpiration = expireTime });

        if (_redisDb != null)
        {
            _redisDb.SetAdd(setCacheKey, token);
            _redisDb.KeyExpire(setCacheKey, expireTime.Value);
        }
        else
        {
            var tokens = _userTokens.GetOrAdd(loginItem.Id, _ => new ConcurrentDictionary<string, byte>());
            tokens.TryAdd(token, 0);
        }

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
        _cache.SetString(cacheKey, SimApiUtil.Json(loginItem));

        if (_redisDb != null)
        {
            var ttl = _redisDb.KeyTimeToLive(cacheKey);
            var setCacheKey = TokenSetCacheKey.Replace("{userId}", loginItem.Id);
            _redisDb.KeyExpire(setCacheKey, ttl);
        }

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
        var login = _cache.GetString(cacheKey);
        var resp = login != null ? SimApiUtil.FromJson<SimApiLoginItem>(login) : null;

        if (resp != null && _redisDb != null)
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
        if (_redisDb != null)
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
                        resp.Add(item);
                    else
                        _redisDb.SetRemove(setCacheKey, login);
                }
            }

            return resp.ToArray();
        }
        else
        {
            if (!_userTokens.TryGetValue(userId, out var tokens))
                return [];

            var resp = new List<SimApiLoginItem>();
            foreach (var token in tokens.Keys)
            {
                var item = GetLogin(token);
                if (item != null)
                    resp.Add(item);
                else
                    tokens.TryRemove(token, out _);
            }

            return resp.ToArray();
        }
    }

    /// <summary>
    /// 退出所有登录
    /// </summary>
    /// <param name="userId"></param>
    public void LogoutAll(string userId)
    {
        if (_redisDb != null)
        {
            var setCacheKey = TokenSetCacheKey.Replace("{userId}", userId);
            var allLogins = _redisDb.SetMembers(setCacheKey).ToStringArray();
            foreach (var login in allLogins)
            {
                if (login != null)
                {
                    var cacheKey = TokenCacheKey.Replace("{token}", login);
                    _cache.Remove(cacheKey);
                }
            }

            _redisDb.KeyDelete(setCacheKey);
        }
        else
        {
            if (_userTokens.TryRemove(userId, out var tokens))
            {
                foreach (var token in tokens.Keys)
                {
                    var cacheKey = TokenCacheKey.Replace("{token}", token);
                    _cache.Remove(cacheKey);
                }
            }
        }
    }

    /// <summary>
    /// 退出登陆
    /// </summary>
    /// <param name="token">登陆标识</param>
    public void Logout(string token)
    {
        var item = GetLogin(token);

        if (_redisDb != null)
        {
            if (item != null)
            {
                var setCacheKey = TokenSetCacheKey.Replace("{userId}", item.Id);
                _redisDb.SetRemove(setCacheKey, token);
            }
        }
        else
        {
            if (item != null && _userTokens.TryGetValue(item.Id, out var tokens))
                tokens.TryRemove(token, out _);
        }

        var cacheKey = TokenCacheKey.Replace("{token}", token);
        _cache.Remove(cacheKey);
    }
}
