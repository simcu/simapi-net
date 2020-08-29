using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using SimApi.Communications;

namespace SimApi.Middlewares
{
    /// <summary>
    /// 认证信息获取中间件
    /// </summary>
    public class SimApiAuthMiddleware
    {
        private RequestDelegate Next { get; }

        public SimApiAuthMiddleware(RequestDelegate next)
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
                    httpContext.Items.Add("LoginInfo", JsonSerializer.Deserialize<SimApiLoginItem>(login));
                }
            }
           
            return Next(httpContext);
        }
    }
}
