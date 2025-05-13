using System;
using System.Linq;
using System.Text;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;

namespace SimApi.Helpers;

public class SimApiJobWebAuth(string user, string pass) : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();
        if (authHeader != null && authHeader.StartsWith("Basic "))
        {
            var encodedUsernamePassword = authHeader.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[1].Trim();
            var decodedUsernamePassword = Encoding.UTF8.GetString(Convert.FromBase64String(encodedUsernamePassword));
            var username = decodedUsernamePassword.Split(':', 2)[0];
            var password = decodedUsernamePassword.Split(':', 2)[1];

            if (username == user && password == pass)
            {
                return true;
            }
        }

        httpContext.Response.StatusCode = 401;
        httpContext.Response.Headers.WWWAuthenticate = "Basic realm=\"SimApiBasicAuth\"";
        httpContext.Response.WriteAsync("").Wait();
        return false;
    }
}