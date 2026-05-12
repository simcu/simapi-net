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
    public virtual  string Server { get; init; } = apiOptions?.SimApiHttpClientOptions.Server ?? string.Empty;
    public virtual  string AppId { get; init; } = apiOptions?.SimApiHttpClientOptions.AppId ?? string.Empty;
    public virtual  string AppKey { get; init; } = apiOptions?.SimApiHttpClientOptions.AppKey ?? string.Empty;

    public virtual  string SignName { get; init; } = "sign";
    public virtual  string TimestampName { get; init; } = "timestamp";
    public virtual  string NonceName { get; init; } = "nonce";
    public virtual  string? AppIdName { get; init; } = "appId";
    public virtual  string[] SignFields { get; init; } = [];

    /// <summary>
    /// 发起签名请求
    /// </summary>
    /// <param name="url"></param>
    /// <param name="body"></param>
    /// <param name="queries"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T? SignQuery<T>(string url, object? body = null, Dictionary<string, string>? queries = null)
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
    public T? AesQuery<T>(string url, object body)
    {
        url = Server + url;
        if (!string.IsNullOrEmpty(AppIdName))
        {
            url += $"?{AppIdName}={AppId}";
        }

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
    public T? AesSignQuery<T>(string url, object body, Dictionary<string, string>? queries = null)
    {
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
    private T? Query<T>(string url, object? req)
    {
        var http = new HttpClient();
        logger?.LogDebug($"[HTTPCLIENT请求] {url}\n{SimApiUtil.Json(req)}\n");
        var resp = http.PostAsJsonAsync(url, req).Result;
        logger?.LogDebug($"[HTTPCLIENT响应] {resp.Content.ReadAsStringAsync().Result}\n");
        var res = resp.Content.ReadFromJsonAsync<SimApiBaseResponse<T>>().Result;
        SimApiError.ErrorWhenNull(res, 500, "请求发生错误");
        SimApiError.ErrorWhen(res.Code != 200, res.Code, res.Message);
        return res.Data;
    }
}