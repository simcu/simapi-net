using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using SimApi.Communications;
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
    public SimApiBaseResponse ExceptionHandler(int code)
    {
        return new SimApiBaseResponse(code);
    }


    [HttpPost, HttpGet]
    public SimApiBaseResponse<Dictionary<string, string>> Versions()
    {
        return new SimApiBaseResponse<Dictionary<string, string>>()
        {
            Data = new Dictionary<string, string>
            {
                { "SimApi", SimApiUtil.SimApiVersion },
                { "App", SimApiUtil.AppVersion }
            }
        };
    }
}