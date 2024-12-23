using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SimApi.Communications;

namespace SimApi.Helpers;

public class SimApiResponseFilter : IResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        context.Result = context.Result switch
        {
            // 检查结果是否为 null
            null => new OkObjectResult(new SimApiBaseResponse()),
            ObjectResult { Value: SimApiBaseResponse simApiBaseResponse } =>
                new OkObjectResult(simApiBaseResponse),
            ObjectResult objectResult => new OkObjectResult(new SimApiBaseResponse<object>(objectResult.Value!)),
            EmptyResult => new OkObjectResult(new SimApiBaseResponse()),
            _ => context.Result
        };
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {
    }
}