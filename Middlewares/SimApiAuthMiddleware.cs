using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using SimApi.Communications;

namespace SimApi.Middlewares;

/// <summary>
/// 认证信息获取中间件
/// </summary>
public class SimApiAuthMiddleware(RequestDelegate next)
{
    public Task Invoke(HttpContext httpContext, IDistributedCache cache)
    {
        string token = null;
        if (httpContext.Request.Headers.TryGetValue("Token", out var header))
        {
            token = header;
        }

        if (!string.IsNullOrEmpty(token))
        {
            var login = cache.GetString(token);
            if (login != null)
            {
                httpContext.Items.Add("LoginInfo", JsonSerializer.Deserialize<SimApiLoginItem>(login));
            }
        }

        return next(httpContext);
    }
}