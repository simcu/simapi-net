using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using SimApi.Helpers;

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
}