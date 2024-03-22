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
    /// 产生一个Token记录并返回Token
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public string Login(string id, string type = "user", string token = null)
    {
        return Login(id, new[] { type }, token);
    }

    /// <summary>
    /// 产生一个Token并记录用户ID角色[多角色]
    /// </summary>
    /// <param name="id"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public string Login(string id, string[] type, string uuid = null)
    {
        uuid ??= Guid.NewGuid().ToString();
        var loginItem = new SimApiLoginItem(id, type);
        cache.SetString(uuid, JsonSerializer.Serialize(loginItem));
        return uuid;
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