using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SimApi.Attributes;
using SimApi.Communications;
using SimApi.Configurations;
using SimApi.Helpers;
using static SimApi.Helpers.SimApiError;

namespace SimApi.Controllers;

public class SimApiCommonController(SimApiOptions simApiOptions) : SimApiBaseController
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
        Error(code);
    }

    /// <summary>
    /// 给前端的自定义信息
    /// </summary>
    /// <returns></returns>
    [HttpPost, HttpGet]
    public Dictionary<string, object> WebConfig()
    {
        var resp = simApiOptions.WebConfig!.ToDictionary();
        if (simApiOptions.WebConfigIncludeVersion)
        {
            resp.Add("Versions", new Dictionary<string, string>
            {
                { "SimApi", SimApiUtil.SimApiVersion },
                { "App", SimApiUtil.AppVersion }
            });
        }

        return resp;
    }


    /// <summary>
    /// 获取已登录用户信息
    /// </summary>
    /// <returns></returns>
    [HttpPost, SimApiAuth, SimApiDoc("认证", "获取已登录用户信息")]
    public SimApiLoginItem UserInfo() => LoginInfo;
}