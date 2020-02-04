using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using YYApi.Communications;

namespace YYApi.Middlewares
{
    /// <summary>
    /// 认证信息获取中间件
    /// </summary>
    public class YYAuthMiddleware
    {
        private RequestDelegate Next { get; }

        public YYAuthMiddleware(RequestDelegate next)
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
                    httpContext.Items.Add("LoginInfo", JsonSerializer.Deserialize<YYLoginItem>(login));
                }
            }

            return Next(httpContext);
        }
    }
}
