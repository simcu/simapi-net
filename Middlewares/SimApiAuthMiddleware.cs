using System.Linq;
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
        var token =
            httpContext.Request.Headers["Token"].FirstOrDefault()
            ?? httpContext.Request.Query["token"].FirstOrDefault();

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