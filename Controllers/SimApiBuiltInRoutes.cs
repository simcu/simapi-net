using System;
using System.Collections.Generic;
using SimApi.Configurations;

namespace SimApi.Controllers;

/// <summary>
/// SimApi 内置端点路由表中的一项。
/// </summary>
public sealed record SimApiBuiltInRoute(
    string RouteName,
    string Controller,
    string Action,
    string[] HttpMethods,
    string Path);

/// <summary>
/// SimApi 内置端点路由表 —— 路径的唯一配置处。
/// UseSimApi 用它注册约定路由(动态路由)，
/// SimApiBuiltInRoutesDescriptionProvider 用它生成文档条目。
/// 某项路径为 null / 空时该端点既不注册路由、也不出现在文档中。
/// </summary>
public static class SimApiBuiltInRoutes
{
    public static IEnumerable<SimApiBuiltInRoute> Get(SimApiRouteOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!string.IsNullOrEmpty(options.UserInfoRoute))
        {
            yield return new SimApiBuiltInRoute(
                "UserInfo", "SimApiCommon", "UserInfo", ["POST"], options.UserInfoRoute);
        }

        if (!string.IsNullOrEmpty(options.LogoutRoute))
        {
            yield return new SimApiBuiltInRoute(
                "Logout", "SimApiAuth", "Logout", ["POST"], options.LogoutRoute);
        }

        if (!string.IsNullOrEmpty(options.WebConfigRoute))
        {
            // 控制器动作同时标注了 HttpGet/HttpPost
            yield return new SimApiBuiltInRoute(
                "WebConfig", "SimApiCommon", "WebConfig", ["GET", "POST"], options.WebConfigRoute);
        }
    }
}
