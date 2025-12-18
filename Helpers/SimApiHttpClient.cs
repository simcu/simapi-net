using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using SimApi.Communications;
using SimApi.Exceptions;

namespace SimApi.Helpers;

public class SimApiHttpClient(string? appId, string appKey, bool debug = false)
{
    public string Server { get; init; } = string.Empty;
    public string SignName { get; init; } = "sign";
    public string TimestampName { get; init; } = "timestamp";
    public string NonceName { get; init; } = "nonce";
    public string? AppIdName { get; init; } = "appId";
    public string[] SignFields { get; init; } = [];

    public T? SignQuery<T>(string url, object? body = null, Dictionary<string, string>? queries = null)
    {
        url = Server + url;
        var queryUrl = SignFields.Aggregate(string.Empty,
            (current, signField) => current + $"{signField}={queries?[signField]}&");
        if (!string.IsNullOrEmpty(AppIdName))
        {
            queryUrl += $"{AppIdName}={appId}&";
        }

        queryUrl += $"{TimestampName}={(int)SimApiUtil.TimestampNow}&{NonceName}={Guid.NewGuid()}";
        var signStr = $"{queryUrl}&{appKey}";
        var path = $"{url}?{queryUrl}&{SignName}={SimApiUtil.Md5(signStr)}";

        if (queries != null)
        {
            path = queries.Where(q => !SignFields.Contains(q.Key))
                .Aggregate(path, (current, q) => current + $"&{q.Key}={q.Value}");
        }

        return Query<T>(path, body);
    }

    public T? AesQuery<T>(string url, object body)
    {
        url = Server + url;
        if (!string.IsNullOrEmpty(AppIdName))
        {
            url += $"?{AppIdName}={appId}";
        }

        var req = new SimApiOneFieldRequest<string>
        {
            Data = SimApiAesUtil.Encrypt(SimApiUtil.Json(body), appKey)
        };
        return Query<T>(url, req);
    }

    public T? AesSignQuery<T>(string url, object body, Dictionary<string, string>? queries = null)
    {
        var req = new SimApiOneFieldRequest<string>
        {
            Data = SimApiAesUtil.Encrypt(SimApiUtil.Json(body), appKey)
        };
        return SignQuery<T>(url, req, queries);
    }

    private T? Query<T>(string url, object? req)
    {
        var http = new HttpClient();
        if (debug)
        {
            Console.WriteLine($"[HTTPCLIENT请求] {url}\n{SimApiUtil.Json(req)}\n");
        }

        var resp = http.PostAsJsonAsync(url, req).Result;
        if (debug)
        {
            Console.WriteLine($"[HTTPCLIENT响应] {resp.Content.ReadAsStringAsync().Result}\n");
        }

        var res = resp.Content.ReadFromJsonAsync<SimApiBaseResponse<T>>().Result;
        if (res == null)
        {
            throw new SimApiException(500, "请求发生错误");
        }

        return res.Code != 200 ? throw new SimApiException(res.Code, res.Message) : res.Data;
    }
}