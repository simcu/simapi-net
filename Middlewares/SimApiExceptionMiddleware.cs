using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SimApi.Communications;
using Microsoft.Extensions.Logging;
using SimApi.Exceptions;

namespace SimApi.Middlewares;

/// <summary>
/// 异常处理中间件
/// </summary>
public class SimApiExceptionMiddleware(RequestDelegate next, ILogger<SimApiExceptionMiddleware> log)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("Query-Id", out var header))
        {
            context.Response.Headers["Query-Id"] = header;
        }

        SimApiBaseResponse response;
        try
        {
            await next(context);
            switch (context.Response.StatusCode)
            {
                case 200:
                case 301:
                case 302:
                    break;
                case 404:
                    throw new SimApiException(context.Response.StatusCode, "请求的接口不存在");
                default:
                    throw new SimApiException(context.Response.StatusCode);
            }
        }
        catch (SimApiException ex)
        {
            response = string.IsNullOrEmpty(ex.Message)
                ? new SimApiBaseResponse(ex.Code)
                : new SimApiBaseResponse(ex.Code, ex.Message);
            ErrorResponse(context, response);
        }
        catch (Exception ex)
        {
            log.LogError("{Msg}", ex.Message);
            log.LogError("{Msg}", ex.StackTrace);
            response = new SimApiBaseResponse(500, ex.Message);
            ErrorResponse(context, response);
        }
    }

    /// <summary>
    /// 异常抛出错误
    /// </summary>
    /// <param name="context"></param>
    /// <param name="response"></param>
    private static void ErrorResponse(HttpContext context, SimApiBaseResponse response)
    {
        context.Response.StatusCode = 200;
        context.Response.Headers.Append("Content-Type", "application/json");
        context.Response.WriteAsync(response.ToString()).Wait();
    }
}