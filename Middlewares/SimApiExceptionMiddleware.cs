using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SimApi.Communications;
using Microsoft.Extensions.Logging;
using SimApi.Exceptions;

namespace SimApi.Middlewares
{
    /// <summary>
    /// 异常处理中间件
    /// </summary>
    public class SimApiExceptionMiddleware
    {
        private RequestDelegate Next { get; }
        private ILogger<SimApiExceptionMiddleware> Log { get; }

        public SimApiExceptionMiddleware(RequestDelegate next, ILogger<SimApiExceptionMiddleware> log)
        {
            Log = log;
            Next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Headers.ContainsKey("Query-Id"))
            {
                context.Response.Headers["Query-Id"] = context.Request.Headers["Query-Id"];
            }

            var response = new SimApiBaseResponse();
            try
            {
                await Next(context);
                if (context.Response.StatusCode != 200)
                {
                    if (!new[] {301, 302}.Contains(context.Response.StatusCode))
                    {
                        throw new SimApiException(context.Response.StatusCode);
                    }
                }
            }
            catch (SimApiException ex)
            {
                if (string.IsNullOrEmpty(ex.Message))
                {
                    response.SetCode(ex.Code);
                }
                else
                {
                    response.SetCodeMsg(ex.Code, ex.Message);
                }

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
        private void ErrorResponse(HttpContext context, SimApiBaseResponse response)
        {
            context.Response.StatusCode = 200;
            context.Response.Headers.Add("Content-Type", "application/json");
            context.Response.WriteAsync(response.ToString());
        }
    }
}