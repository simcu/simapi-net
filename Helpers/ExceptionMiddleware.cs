using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using YYApi.Communications;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;

namespace YYApi.Helpers
{
    /// <summary>
    /// 异常处理中间件
    /// </summary>
    public class ExceptionMiddleware
    {
        private RequestDelegate Next { get; }
        private ILogger<ExceptionMiddleware> Log { get; }

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> log)
        {
            Log = log;
            Next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var response = new BaseResponse();
            try
            {
                await Next(context);
            }
            catch (ApiException ex)
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
        private void ErrorResponse(HttpContext context, BaseResponse response)
        {
            context.Response.StatusCode = 200;
            context.Response.Headers.Add("Content-Type", "application/json");
            context.Response.WriteAsync(response.ToString());
        }
    }


    /// <summary>
    /// Api错误捕获异常
    /// </summary>
    public class ApiException : Exception
    {
        public int Code { get; }

        public ApiException(int code, string message = "") : base(message)
        {
            Code = code;
        }
    }
}