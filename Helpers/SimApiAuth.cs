using System;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using SimApi.Communications;

namespace SimApi.Helpers;

/// <summary>
/// 认证助手
/// </summary>
public class SimApiAuth(IDistributedCache cache)
{
    /// <summary>
    /// 登录信息
    /// </summary>
    /// <param name="loginItem"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public string Login(SimApiLoginItem loginItem, string? token = null)
    {
        token ??= Guid.NewGuid().ToString();
        cache.SetString(token, JsonSerializer.Serialize(loginItem));
        return token;
    }

    /// <summary>
    /// 获取登陆信息
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public SimApiLoginItem? GetLogin(string token)
    {
        var login = cache.GetString(token);
        return login != null ? JsonSerializer.Deserialize<SimApiLoginItem>(login) : null;
    }

    /// <summary>
    /// 退出登陆
    /// </summary>
    /// <param name="uuid">登陆标识</param>
    public void Logout(string uuid)
    {
        if (!string.IsNullOrEmpty(uuid))
        {
            cache.Remove(uuid);
        }
    }
}