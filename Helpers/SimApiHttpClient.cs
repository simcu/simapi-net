using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using SimApi.Communications;
using SimApi.Configurations;
using SimApi.Exceptions;

namespace SimApi.Helpers;

public class SimApiHttpClient(SimApiOptions? apiOptions = null, ILogger<SimApiHttpClient>? logger = null)
{
    //********** 共享 HttpClient **************
    // 以前每次 Query 都 new HttpClient()，等于每次调用都要重新做 DNS + TCP + TLS 握手。
    // 跨网络调用时一次握手就能吃掉几秒（尤其是域名解析到远端时），叠加多次调用会直接超时。
    // 这里改为全局单例复用连接池：只有第一个请求付握手成本，后续走 keep-alive。

    /// <summary>
    /// 单个请求的整体超时
    /// </summary>
    public static TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// 建立连接的超时。目标不可达时快速失败，而不是把调用方一直挂住。
    /// </summary>
    public static TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(10);

    private static readonly Lazy<HttpClient> SharedHttp = new(CreateHttpClient, isThreadSafe: true);

    private static HttpClient CreateHttpClient()
    {
        var handler = new SocketsHttpHandler
        {
            // 连接最多复用 5 分钟，到期重建，让 DNS 变更（内网/公网切换）能生效
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            ConnectTimeout = ConnectTimeout,
            AutomaticDecompression = System.Net.DecompressionMethods.All,
        };
        return new HttpClient(handler) { Timeout = RequestTimeout };
    }

    public virtual string Server { get; init; } = apiOptions?.SimApiHttpClientOptions.Server ?? string.Empty;
    public virtual string AppId { get; init; } = apiOptions?.SimApiHttpClientOptions.AppId ?? string.Empty;
    public virtual string AppKey { get; init; } = apiOptions?.SimApiHttpClientOptions.AppKey ?? string.Empty;

    public virtual string SignName { get; init; } = "sign";
    public virtual string TimestampName { get; init; } = "timestamp";
    public virtual string NonceName { get; init; } = "nonce";
    public virtual string? AppIdName { get; init; } = "appId";
    public virtual string[] SignFields { get; init; } = [];

    /// <summary>
    /// 发起签名请求
    /// </summary>
    /// <param name="url"></param>
    /// <param name="body"></param>
    /// <param name="queries"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T SignQuery<T>(string url, object? body = null, Dictionary<string, string>? queries = null)
    {
        url = Server + url;
        var queryUrl = SignFields.Aggregate(string.Empty,
            (current, signField) => current + $"{signField}={queries?[signField]}&");
        if (!string.IsNullOrEmpty(AppIdName))
        {
            queryUrl += $"{AppIdName}={AppId}&";
        }

        queryUrl += $"{TimestampName}={(int)SimApiUtil.TimestampNow}&{NonceName}={Guid.NewGuid()}";
        var signStr = $"{queryUrl}&{AppKey}";
        var path = $"{url}?{queryUrl}&{SignName}={SimApiUtil.Md5(signStr)}";

        if (queries != null)
        {
            path = queries.Where(q => !SignFields.Contains(q.Key))
                .Aggregate(path, (current, q) => current + $"&{q.Key}={q.Value}");
        }

        return Query<T>(path, body);
    }

    /// <summary>
    /// 发起AES加密请求
    /// </summary>
    /// <param name="url"></param>
    /// <param name="body"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T AesQuery<T>(string url, object body)
    {
        url = Server + url;
        if (!string.IsNullOrEmpty(AppIdName))
        {
            url += $"?{AppIdName}={AppId}";
        }
        logger?.LogDebug($"[HTTPCLIENT请求][加密前BODY] {url}\n{SimApiUtil.Json(body)}\n");
        var req = new SimApiOneFieldRequest<string>
        {
            Data = SimApiAesUtil.Encrypt(SimApiUtil.Json(body), AppKey)
        };
        return Query<T>(url, req);
    }

    /// <summary>
    /// 发起AES加密以及签名请求
    /// </summary>
    /// <param name="url"></param>
    /// <param name="body"></param>
    /// <param name="queries"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T AesSignQuery<T>(string url, object body, Dictionary<string, string>? queries = null)
    {
        logger?.LogDebug($"[HTTPCLIENT请求][加密前BODY] {url}\n{SimApiUtil.Json(body)}\n");
        var req = new SimApiOneFieldRequest<string>
        {
            Data = SimApiAesUtil.Encrypt(SimApiUtil.Json(body), AppKey)
        };
        return SignQuery<T>(url, req, queries);
    }

    /// <summary>
    /// 发起请求
    /// </summary>
    /// <param name="url"></param>
    /// <param name="req"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="SimApiException"></exception>
    private T Query<T>(string url, object? req)
    {
        // 共享实例，不要 using/Dispose，否则会破坏连接池
        var http = SharedHttp.Value;

        // 字符串插值里的 Json 序列化开销不小，关闭 Debug 时不应该白白付出
        var debug = logger?.IsEnabled(LogLevel.Debug) == true;
        if (debug) logger!.LogDebug($"[HTTPCLIENT请求] {url}\n{SimApiUtil.Json(req)}\n");

        var resp = http.PostAsJsonAsync(url, req).Result;

        if (debug) logger!.LogDebug($"[HTTPCLIENT响应] {resp.Content.ReadAsStringAsync().Result}\n");

        SimApiError.ErrorWhenFalse(resp.IsSuccessStatusCode, (int)resp.StatusCode, $"HTTP ERROR: {resp.StatusCode}");
        var res = resp.Content.ReadFromJsonAsync<SimApiBaseResponse<T>>().Result;
        SimApiError.ErrorWhenNull(res, 500, "请求发生错误");
        SimApiError.ErrorWhen(res.Code != 200, res.Code, res.Message);
        return res.Data!;
    }
}