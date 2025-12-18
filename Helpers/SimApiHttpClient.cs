using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using SimApi.Communications;

namespace SimApi.Helpers;

public class SimApiHttpClient(string? appId, string appKey)
{
    public string Server { get; init; } = string.Empty;
    public string SignName { get; init; } = "sign";
    public string TimestampName { get; init; } = "timestamp";
    public string NonceName { get; init; } = "nonce";
    public string? AppIdName { get; init; } = "appId";
    public string[] SignFields { get; init; } = [];


    public T? SignQuery<T>(string url, object body, Dictionary<string, string>? queries = null)
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

        var http = new HttpClient();
        var resp = http.PostAsJsonAsync(path, body).Result;
        return resp.Content.ReadFromJsonAsync<T>().Result;
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
        var http = new HttpClient();
        var resp = http.PostAsJsonAsync(url, req).Result;
        return resp.Content.ReadFromJsonAsync<T>().Result;
    }

    public T? AesSignQuery<T>(string url, object body, Dictionary<string, string>? queries = null)
    {
        var req = new SimApiOneFieldRequest<string>
        {
            Data = SimApiAesUtil.Encrypt(SimApiUtil.Json(body), appKey)
        };
        return SignQuery<T>(url, req, queries);
    }
}