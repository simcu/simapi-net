using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using YYApi.Communications;
using Microsoft.Extensions.Logging;
using YYApi.Exceptions;

namespace YYApi.Middlewares
{
    /// <summary>
    /// 异常处理中间件
    /// </summary>
    public class YYExceptionMiddleware
    {
        private RequestDelegate Next { get; }
        private ILogger<YYExceptionMiddleware> Log { get; }

        public YYExceptionMiddleware(RequestDelegate next, ILogger<YYExceptionMiddleware> log)
        {
            Log = log;
            Next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var response = new YYBaseResponse();
            try
            {
                await Next(context);
            }
            catch (YYApiException ex)
            {
                response.SetCodeMsg(ex.Code, ex.Message);
                ErrorResponse(context, response);
            }
            catch (Exception ex)
            {
                Log.LogError(ex.Message);
                Log.LogError(ex.StackTrace);
                response.SetCodeMsg(500, ex.Message);
                ErrorResponse(context, response);
            }
        }

        /// <summary>
        /// 异常抛出错误
        /// </summary>
        /// <param name="context"></param>
        /// <param name="response"></param>
        private void ErrorResponse(HttpContext context, YYBaseResponse response)
        {
            context.Response.StatusCode = 200;
            context.Response.Headers.Add("Content-Type", "application/json");
            context.Response.WriteAsync(response.ToString());
        }
    }
}