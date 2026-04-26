using Microsoft.AspNetCore.Mvc;
using SimApi.Attributes;
using SimApi.Communications;
using SimApi.Helpers;

namespace SimApi.Controllers;

using static SimApiError;

public class SimApiAuthController(SimApiAuth auth) : SimApiBaseController
{
    /// <summary>
    /// 检测用户登陆的控制器
    /// </summary>
    /// <returns></returns>
    [HttpPost, SimApiDoc("认证", "检测登陆")]
    public string CheckLogin()
    {
        ErrorWhenNull(LoginInfo, 401, "未登录");
        return LoginInfo.Id;
    }

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


    [HttpPost, SimApiAuth, SimApiDoc("认证", "获取已登录用户信息")]
    public SimApiLoginItem UserInfo() => LoginInfo;
}