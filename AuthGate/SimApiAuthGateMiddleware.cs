using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SimApi.Communications;
using SimApi.Configurations;
using SimApi.Helpers;

namespace SimApi.AuthGate;

public class SimApiAuthGateMiddleware(RequestDelegate next, ILogger<SimApiAuthGateMiddleware> logger)
{
    public Task Invoke(HttpContext httpContext, SimApiOptions simApiOptions)
    {
        if (httpContext.Request.Headers.TryGetValue("X-SimApi-Gate-Auth", out var auth) &&
            httpContext.Request.Headers.TryGetValue("X-SimApi-Gate-Time", out var time) &&
            httpContext.Request.Headers.TryGetValue("X-SimApi-Gate-Sign", out var sign))
        {
            var signStr =
                $"appId={simApiOptions.SimApiAuthGateOptions.AppId}&auth={auth}&time={time}&appKey={simApiOptions.SimApiAuthGateOptions.AppKey}";
            logger.LogDebug($"签名字符串 => {signStr}");
            if (SimApiUtil.Md5(signStr) == sign && !string.IsNullOrEmpty(auth))
            {
                var login = SimApiUtil.Base64Decode<SimApiLoginItem>(auth!);
                httpContext.Items.Add("LoginInfo", login);
            }
            else
            {
                logger.LogDebug("签名不匹配");
            }
        }

        return next(httpContext);
    }
}