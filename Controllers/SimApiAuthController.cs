using Microsoft.AspNetCore.Mvc;
using SimApi.Attributes;
using SimApi.Communications;
using SimApi.Helpers;

namespace SimApi.Controllers;

using static SimApiError;

public class SimApiAuthController(SimApiAuth auth) : SimApiBaseController
{
    /// <summary>
    /// 退出登陆
    /// </summary>
    /// <returns></returns>
    [HttpPost, SimApiDoc("认证", "退出登陆")]
    public void Logout()
    {
        if (Request.Headers.TryGetValue("Token", out var value))
        {
            auth.Logout(value!);
        }
    }
}