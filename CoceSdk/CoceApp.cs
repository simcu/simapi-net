using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using SimApi.Communications;
using SimApi.Configurations;
using SimApi.Helpers;

namespace SimApi.CoceSdk;

public class CoceApp(SimApiOptions simApiOptions, ILogger<CoceApp> logger, IDistributedCache cache)
{
    
    /// <summary>
    /// 获取Level Token
    /// </summary>
    /// <param name="lv1Token"></param>
    /// <param name="level"></param>
    /// <returns></returns>
    public LevelTokenResponse? GetLevelToken(string lv1Token, int level = 5)
    {
        var dict = new Dictionary<string, object>
        {
            { "lv1Token", lv1Token },
            { "time", (int)SimApiUtil.TimestampNow },
            { "appId", simApiOptions.CoceSdkOptions.AppId! },
            { "level", level }
        };
        return QueryAppApi<LevelTokenResponse>("/api/app/token", dict);
    }

    /// <summary>
    /// 通过用户手机号搜索用户
    /// </summary>
    /// <param name="phone"></param>
    /// <returns></returns>
    public UserInfo? SearchUserByPhone(string phone)
    {
        var dict = new Dictionary<string, object>
        {
            { "cell", phone },
            { "time", (int)SimApiUtil.TimestampNow },
            { "appId", simApiOptions.CoceSdkOptions.AppId! },
        };
        return QueryAppApi<UserInfo>("/api/app/user/search-by-phone", dict);
    }

    /// <summary>
    /// 通过给出的UserId列表获取用户信息
    /// </summary>
    /// <param name="userIds"></param>
    /// <returns></returns>
    public UserInfo[]? SearchUserByIds(IEnumerable<string> userIds)
    {
        var dict = new Dictionary<string, object>
        {
            { "ids", string.Join(",", userIds) },
            { "time", (int)SimApiUtil.TimestampNow },
            { "appId", simApiOptions.CoceSdkOptions.AppId! },
        };
        return QueryAppApi<UserInfo[]>("/api/app/user/search-by-ids", dict);
    }

    /// <summary>
    /// 向用户发送消息
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="title"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    public bool SendUserMessage(string userId, string title, string content)
    {
        var dict = new Dictionary<string, object>
        {
            { "time", (int)SimApiUtil.TimestampNow },
            { "appId", simApiOptions.CoceSdkOptions.AppId! },
            { "userId", userId },
            { "type", "text" },
            { "title", title },
            { "text", content }
        };
        return QueryAppApiNoResp("/api/app/message", dict);
    }

    /// <summary>
    /// 创建交易订单号
    /// </summary>
    /// <param name="name"></param>
    /// <param name="amount"></param>
    /// <param name="ext"></param>
    /// <returns></returns>
    public string? TradeCreate(string name, int amount, string ext)
    {
        var dict = new Dictionary<string, object>
        {
            { "time", (int)SimApiUtil.TimestampNow },
            { "appId", simApiOptions.CoceSdkOptions.AppId! },
            { "amount", amount },
            { "ext", ext },
            { "name", name }
        };
        return QueryAppApi<string>("/api/app/trade/create", dict);
    }

    /// <summary>
    /// 查询订单状态
    /// </summary>
    /// <param name="tradeNo"></param>
    /// <returns></returns>
    public CheckTradeResponse? TradeCheck(string tradeNo)
    {
        var dict = new Dictionary<string, object>
        {
            { "time", (int)SimApiUtil.TimestampNow },
            { "appId", simApiOptions.CoceSdkOptions.AppId! },
            { "tradeNo", tradeNo }
        };
        return QueryAppApi<CheckTradeResponse>("/api/app/trade/result", dict);
    }

    /// <summary>
    /// 对订单进行退款
    /// </summary>
    /// <param name="tradeNo"></param>
    /// <returns></returns>
    public bool TradeRefund(string tradeNo)
    {
        var dict = new Dictionary<string, object>
        {
            { "time", (int)SimApiUtil.TimestampNow },
            { "appId", simApiOptions.CoceSdkOptions.AppId! },
            { "tradeNo", tradeNo }
        };
        return QueryAppApiNoResp("/api/app/trade/refund", dict);
    }


    private T? QueryAppApi<T>(string endpoint, Dictionary<string, object> request)
    {
        var response = QueryAppApi(endpoint, request);
        var result = response.Content.ReadFromJsonAsync<SimApiBaseResponse<T>>().Result!;
        if (result.Code == 200) return result.Data;
        logger.LogDebug("发生错误: {Code} => {Message}", result.Code, result.Message);
        return default;
    }

    private bool QueryAppApiNoResp(string endpoint, Dictionary<string, object> request)
    {
        var response = QueryAppApi(endpoint, request);
        var result = response.Content.ReadFromJsonAsync<SimApiBaseResponse>().Result!;
        return result.Code == 200;
    }

    private HttpResponseMessage QueryAppApi(string endpoint, Dictionary<string, object> request)
    {
        var platUrl = simApiOptions.CoceSdkOptions.ApiEndpoint + endpoint;
        var sorted = request.OrderBy(x => x.Key);
        var signStr = sorted.Aggregate("", (current, item) => current + $"{item.Key}={item.Value}&").TrimEnd('&');
        logger.LogDebug("签名的字符串: {SignStr}", signStr);
        var sign = SimApiUtil.Md5(signStr + simApiOptions.CoceSdkOptions.AppKey);
        logger.LogDebug("签名: {Sign}", sign);
        request.Add("sign", sign);
        logger.LogDebug("请求地址: {PlatUrl} => {Data}", platUrl, JsonSerializer.Serialize(request));
        var http = new HttpClient();
        return http.PostAsJsonAsync(platUrl, request).Result;
    }


    /// <summary>
    /// 获取用户的群组信息
    /// </summary>
    /// <param name="token">Level >=2 的Token</param>
    /// <returns></returns>
    public IEnumerable<GroupInfo>? GetUserGroups(string token)
    {
        const string uri = "/api/lv2/user/groups";
        var resp = ProxyQuery<GroupInfo[]>(uri, token,"{}");
        return resp;
    }

    /// <summary>
    /// 获取用户信息
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public UserInfo? GetUserInfo(string token)
    {
        const string uri = "/api/lv1/user/info";
        return ProxyQuery<UserInfo>(uri, token);
    }

    public void SaveToken(string userId, string levelToken)
    {
        cache.SetString($"LEVEL:TOKEN:{userId}", levelToken);
    }

    public string? GetToken(string userId)
    {
        return cache.GetString($"LEVEL:TOKEN:{userId}");
    }


    public dynamic? ProxyQuery(string uri, string token, string json) => ProxyQuery<dynamic>(uri, token, json);

    public dynamic? ProxyQueue(string uri, string token, object data) =>
        ProxyQuery<dynamic>(uri, token, JsonSerializer.Serialize(data));

    public T? ProxyQueue<T>(string uri, string token, object data) =>
        ProxyQuery<T>(uri, token, JsonSerializer.Serialize(data));

    public T? ProxyQuery<T>(string uri, string token, string json = "{}")
    {
        var http = new HttpClient();
        http.DefaultRequestHeaders.Add("Token", token);
        var realUrl = simApiOptions.CoceSdkOptions.ApiEndpoint + uri;
        var response = http.PostAsync(realUrl, new StringContent(json, Encoding.UTF8, "application/json"))
            .Result;
        var resp = response.Content.ReadFromJsonAsync<SimApiBaseResponse<T>>().Result!;
        if (resp.Code != 200)
        {
            logger.LogDebug("请求发生错误: {RespCode} => {RespMessage}", resp.Code, resp.Message);
        }

        return resp.Code == 200 ? resp.Data : default;
    }

    public ConfigResponse GetConfig()
    {
        return new ConfigResponse(simApiOptions.CoceSdkOptions.AppId!, simApiOptions.CoceSdkOptions.AuthEndpoint);
    }
}