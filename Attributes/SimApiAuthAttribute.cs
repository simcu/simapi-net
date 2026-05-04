using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using SimApi.Communications;
using SimApi.Exceptions;
using SimApi.Interfaces;
using static SimApi.Helpers.SimApiError;

namespace SimApi.Attributes;

/// <summary>
/// 检测登录中间件
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SimApiAuthAttribute : ActionFilterAttribute
{
    private string[]? Types { get; }


    //默认是user登录类型
    public SimApiAuthAttribute()
    {
        Types = null;
    }

    //只检测一种用户类型的快捷方式
    public SimApiAuthAttribute(string type)
    {
        Types = type.Split(",");
    }

    //设定特定类型的检测
    public SimApiAuthAttribute(string[] types)
    {
        Types = types;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var loginInfo = (SimApiLoginItem)context.HttpContext.Items["LoginInfo"]!;
        var token = (string)context.HttpContext.Items["LoginToken"]!;
        ErrorWhenNull(loginInfo, 401);
        var checkers = context.HttpContext.RequestServices.GetServices<ISimApiAuthChecker>();
        foreach (var checker in checkers)
        {
            checker.Run(loginInfo, token);
        }

        if (Types == null) return;
        //检测用户类型
        ErrorWhenFalse(Types.Intersect(loginInfo.Type).Any(), 403);
    }
}