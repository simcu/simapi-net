﻿using Microsoft.AspNetCore.Mvc;
using SimApi.Attributes;
using SimApi.Communications;
using SimApi.Helpers;

namespace SimApi.Controllers;

[ApiExplorerSettings(GroupName = "api")]
public class SimApiCommonController(SimApiAuth auth) : SimApiBaseController
{
    /// <summary>
    /// 错误回馈页面
    /// </summary>
    /// <param name="code">错误代码</param>
    /// <returns></returns>
    [HttpGet("exception/{code:int}")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public SimApiBaseResponse ExceptionHandler(int code)
    {
        return new SimApiBaseResponse(code);
    }

    /// <summary>
    /// 检测用户登陆的控制器
    /// </summary>
    /// <returns></returns>
    [HttpPost("/auth/check"), SimApiDoc("认证", "检测登陆")]
    public SimApiBaseResponse<string> CheckLogin()
    {
        ErrorWhenNull(LoginInfo, 401);
        return new SimApiBaseResponse<string>(LoginInfo.Id);
    }

    /// <summary>
    /// 退出登陆
    /// </summary>
    /// <returns></returns>
    [HttpPost("/auth/logout"), SimApiDoc("认证", "退出登陆")]
    public SimApiBaseResponse Logout()
    {
        string token = null;

        if (Request.Headers.TryGetValue("Token", out var value))
        {
            token = value;
        }

        auth.Logout(token);
        return new SimApiBaseResponse();
    }
}