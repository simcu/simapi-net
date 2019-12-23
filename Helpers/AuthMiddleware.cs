using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;

namespace YYApi.Helpers
{
    /// <summary>
    /// 认证信息获取中间件
    /// </summary>
    public class AuthMiddleware
    {
        private RequestDelegate Next { get; }

        public AuthMiddleware(RequestDelegate next)
        {
            Next = next;
        }

        public Task Invoke(HttpContext httpContext, IDistributedCache cache)
        {
            string token = null;
            if (httpContext.Request.Headers.ContainsKey("Token"))
            {
                token = httpContext.Request.Headers["Token"];
            }

            if (!string.IsNullOrEmpty(token))
            {
                var id = cache.GetString(token);
                if (id != null)
                {
                    httpContext.Items.Add("LoginId", id);
                }
            }

            return Next(httpContext);
        }
    }

    /// <summary>
    /// 检测登录中间件
    /// </summary>
    public class CheckAuthAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.HttpContext.Items["LoginId"] == null)
            {
                throw new ApiException(401);
            }
        }
    }

    /// <summary>
    /// 认证助手
    /// </summary>
    public class Auth
    {
        private IDistributedCache Cache { get; }

        public Auth(IDistributedCache cache)
        {
            Cache = cache;
        }

        /// <summary>
        /// 产生一个Token记录并返回Token
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public string SetId(int id)
        {
            var uuid = GetUUID();
            Cache.SetString(uuid, id.ToString());
            return uuid;
        }

        private string GetUUID()
        {
            return Guid.NewGuid().ToString();
        }
    }
}