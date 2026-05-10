using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SimApi.Communications;
using Microsoft.Extensions.Logging;
using SimApi.Configurations;
using SimApi.Exceptions;
using SimApi.Helpers;

namespace SimApi.Middlewares;

/// <summary>
/// 异常处理中间件
/// </summary>
public class SimApiExceptionMiddleware(
    RequestDelegate next,
    ILogger<SimApiExceptionMiddleware> log,
    SimApiOptions simApiOptions)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("Query-Id", out var header))
        {
            context.Response.Headers["Query-Id"] = header;
        }

        try
        {
            await next(context);
            if (!context.Response.HasStarted)
            {
                SimApiError.ErrorWhenFalse(
                    simApiOptions.SimApiExceptionOptions.SkipStatusCodes.Contains(context.Response.StatusCode),
                    context.Response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            // 解包异步异常
            ex = UnwrapAggregateException(ex);
            SimApiBaseResponse response;
            if (ex is SimApiException simEx)
            {
                response = string.IsNullOrEmpty(simEx.Message)
                    ? new SimApiBaseResponse(simEx.Code)
                    : new SimApiBaseResponse(simEx.Code, simEx.Message);
            }
            else
            {
                log.LogError(ex, "服务器异常");
                response = new SimApiBaseResponse(500, "服务器错误");
            }

            await ErrorResponseAsync(context, response);
        }
    }

    private static Exception UnwrapAggregateException(Exception ex)
    {
        while (ex is AggregateException aggEx && aggEx.InnerException != null)
        {
            ex = aggEx.InnerException;
        }

        return ex;
    }

    /// <summary>
    /// 异步输出错误响应（修复异步异常捕获核心）
    /// </summary>
    private static async Task ErrorResponseAsync(HttpContext context, SimApiBaseResponse response)
    {
        // 响应已开始则直接返回，不修改
        if (context.Response.HasStarted)
            return;

        context.Response.StatusCode = 200;
        context.Response.ContentType = "application/json";
        context.Response.ContentLength = null; // 清除可能已设置的 Content-Length
        await context.Response.WriteAsync(response.ToString());
    }
}