using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using SimApi.Attributes;
using SimApi.Helpers;
using static SimApi.Helpers.SimApiError;

namespace SimApi.Controllers;

public class SimApiCommonController : SimApiBaseController
{
    /// <summary>
    /// 错误回馈页面
    /// </summary>
    /// <param name="code">错误代码</param>
    /// <returns></returns>
    [HttpGet("exception/{code:int}")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public void ExceptionHandler(int code)
    {
        SimApiError.Error(code);
    }


    [HttpPost, HttpGet]
    public Dictionary<string, string> Versions()
    {
        return new Dictionary<string, string>
        {
            { "SimApi", SimApiUtil.SimApiVersion },
            { "App", SimApiUtil.AppVersion }
        };
    }

    /// <summary>
    /// 检测用户登陆的控制器
    /// </summary>
    /// <returns></returns>
    [HttpPost, SimApiAuth, SimApiDoc("认证", "检测登陆")]
    public string CheckLogin()
    {
        ErrorWhenNull(LoginInfo, 401, "未登录");
        return LoginInfo.Id;
    }
}