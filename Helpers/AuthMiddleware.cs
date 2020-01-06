using System;
using System.Text.Json;
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
                var login = cache.GetString(token);
                if (login != null)
                {
                    httpContext.Items.Add("LoginInfo", JsonSerializer.Deserialize<LoginInfoItem>(login));
                }
            }

            return Next(httpContext);
        }
    }

    /// <summary>
    /// 登录信息中间件
    /// </summary>
    public class LoginInfoItem
    {
        //登录用户的ID
        public int Id { get; set; }
        //登录用户来源
        public string Type { get; set; }
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
        public string SetId(int id, string type = "user")
        {
            var uuid = GetUUID();
            var loginItem = new LoginInfoItem
            {
                Id = id,
                Type = type
            };
            Cache.SetString(uuid, JsonSerializer.Serialize(loginItem));
            return uuid;
        }

        private string GetUUID()
        {
            return Guid.NewGuid().ToString();
        }
    }
}