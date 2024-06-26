#nullable enable
using System;
using System.Collections.Generic;
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
    /// 产生一个Token记录并返回Token
    /// </summary>
    /// <param name="id"></param>
    /// <param name="type"></param>
    /// <param name="meta"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public string Login(string id, Dictionary<string, string>? meta = null, string type = "user", string? token = null)
    {
        return Login(id, meta, new[] { type }, token);
    }

    /// <summary>
    /// 产生一个Token并记录用户ID角色[多角色]
    /// </summary>
    /// <param name="id"></param>
    /// <param name="type"></param>
    /// <param name="meta"></param>
    /// <param name="uuid"></param>
    /// <returns></returns>
    // ReSharper disable once MemberCanBePrivate.Global
    public string Login(string id, Dictionary<string, string>? meta, string[] type, string? uuid = null)
    {
        uuid ??= Guid.NewGuid().ToString();
        var loginItem = new SimApiLoginItem(id, type, meta);
        cache.SetString(uuid, JsonSerializer.Serialize(loginItem));
        return uuid;
    }

    /// <summary>
    /// 设置登录的Meta信息
    /// </summary>
    /// <param name="token"></param>
    /// <param name="meta"></param>
    /// <returns></returns>
    public bool SetMeta(string token, Dictionary<string, string> meta)
    {
        var login = GetLogin(token);
        if (login == null) { return false; }
        var newLogin = new SimApiLoginItem(login.Id, login.Type, login.Meta);
        cache.SetString(token,JsonSerializer.Serialize(newLogin));
        return true;
    }

    /// <summary>
    /// 获取登陆信息
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public SimApiLoginItem? GetLogin(string token)
    {
        var login = cache.GetString(token);
        return login != null ? JsonSerializer.Deserialize<SimApiLoginItem>(login) : default;
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