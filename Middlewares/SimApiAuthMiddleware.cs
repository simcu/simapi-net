using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SimApi.Helpers;

namespace SimApi.Middlewares;

/// <summary>
/// 认证信息获取中间件
/// </summary>
public class SimApiAuthMiddleware(RequestDelegate next)
{
    public Task Invoke(HttpContext httpContext, SimApiAuth auth)
    {
        string? token = null;
        if (httpContext.Request.Headers.TryGetValue("Token", out var header))
        {
            token = header;
        }

        if (string.IsNullOrEmpty(token)) return next(httpContext);
        var login = auth.GetLogin(token);
        if (login != null)
        {
            httpContext.Items.Add("LoginToken", token);
            httpContext.Items.Add("LoginInfo", login);
        }

        return next(httpContext);
    }
}