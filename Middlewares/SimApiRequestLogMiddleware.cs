using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SimApi.Configurations;
using SimApi.Helpers;

namespace SimApi.Middlewares;

/// <summary>
/// 请求日志中间件
/// </summary>
public class SimApiRequestLogMiddleware(
    RequestDelegate next,
    ILogger<SimApiRequestLogMiddleware> log,
    SimApiRequestLogOptions options)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        var fullUrl =
            $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}";

        var logMessage = new StringBuilder();
        logMessage.AppendLine($"[{context.Request.Method}] {fullUrl}");

        if (options.ShowFullHeader)
        {
            logMessage.AppendLine("*( RequestHeaders [Full] ) =>");
            var headersDict = new Dictionary<string, string>();
            foreach (var header in context.Request.Headers)
            {
                headersDict[header.Key] = header.Value.ToString();
            }

            logMessage.AppendLine(JsonSerializer.Serialize(headersDict));
        }
        else
        {
            logMessage.AppendLine("*( RequestHeaders ) =>");
            var token = context.Request.Headers["Token"].FirstOrDefault() ?? "";
            var queryId = context.Request.Headers["Query-Id"].FirstOrDefault() ?? "";
            logMessage.AppendLine($"Token: {token}  QueryId: {queryId}");
        }

        context.Request.EnableBuffering();
        var requestBodyText = await new StreamReader(context.Request.Body).ReadToEndAsync();
        context.Request.Body.Seek(0, SeekOrigin.Begin);
        logMessage.AppendLine("*( RequestBody ) =>");
        if (options.RequestStringLogLength == 0)
        {
            logMessage.AppendLine(requestBodyText);
        }
        else
        {
            // GET/DELETE 等无 body 的请求, 或表单/文件上传等非 JSON body:
            // 不做长度裁剪, 原样记录, 避免空 JSON 解析抛异常
            logMessage.AppendLine(TrimJsonStringFields(requestBodyText, options.RequestStringLogLength));
        }

        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        ExceptionDispatchInfo? edi = null;

        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            edi = ExceptionDispatchInfo.Capture(ex);
        }
        finally
        {
            sw.Stop();

            responseBody.Seek(0, SeekOrigin.Begin);
            var responseText = await new StreamReader(responseBody).ReadToEndAsync();

            logMessage.AppendLine($"*( Response [{context.Response.StatusCode}] ) =>");
            if (options.ResponseStringLogLength > 0)
            {
                // 与请求体相同的字段级裁剪: JSON 按字段截断超长字符串, 非 JSON/空响应原样记录
                logMessage.Append(TrimJsonStringFields(responseText, options.ResponseStringLogLength));
            }
            else if (options.ShowFullResponse)
            {
                logMessage.Append(responseText);
            }
            else
            {
                var truncated = responseText.Length > 200 ? responseText[..200] : responseText;
                logMessage.Append(truncated);
            }

            if (edi != null)
            {
                logMessage.Append(Environment.NewLine);
                logMessage.Append($"Exception: {edi.SourceException}");
            }

            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream;

            log.LogInformation(logMessage.ToString());
        }

        edi?.Throw();
    }

    /// <summary>
    /// 与请求体相同的字段级裁剪:
    /// JSON body 则把长度超过 maxLength 的字符串字段截断为前 maxLength 字符并追加 ...(原长度);
    /// 空 body 或非 JSON body(表单/文件上传等)原样返回, 不裁剪
    /// </summary>
    private static string TrimJsonStringFields(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        Dictionary<string, object>? logs;
        try
        {
            logs = SimApiUtil.FromJson<Dictionary<string, object>>(text);
        }
        catch (JsonException)
        {
            return text;
        }

        if (logs == null)
        {
            return text;
        }

        // 遍历键快照后再赋值, 避免枚举过程中修改 Dictionary 引发异常
        foreach (var key in logs.Keys.ToList())
        {
            if (logs[key] is not JsonElement { ValueKind: JsonValueKind.String } je) continue;
            var str = je.GetString();
            if (str?.Length > maxLength)
            {
                logs[key] = str[..maxLength] + $"...({str.Length})";
            }
        }

        return SimApiUtil.Json(logs);
    }
}