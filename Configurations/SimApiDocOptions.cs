using System.Collections.Generic;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace SimApi.Configurations;

/// <summary>
/// 文档组配置
/// </summary>
public class SimApiDocGroupOption
{
    /// <summary>
    /// 文档标识
    /// </summary>
    public string Id { get; set; } = null!;

    /// <summary>
    /// 文档名称
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// 文档描述
    /// </summary>
    public string Description { get; set; } = null!;
}

/// <summary>
/// 授权配置, Type支持 "SimApiAuth","ClientCredentials","Implicit","AuthorizationCode"
/// </summary>
public class SimApiAuthOption
{
    public string[] Type { get; set; } = new[] { "SimApiAuth" };

    public string Description { get; set; } = "认证服务器颁发的AccessToken";

    public string AuthorizationUrl { get; set; } = null!;

    public string TokenUrl { get; set; } = null!;

    public Dictionary<string, string> Scopes { get; set; } = null!;
}

/// <summary>
/// 文档配置
/// </summary>
public class SimApiDocOptions
{
    /// <summary>
    /// 文档组配置
    /// </summary>
    public SimApiDocGroupOption[] ApiGroups { get; set; } = new[]
    {
        new SimApiDocGroupOption
        {
            Id = "api",
            Name = "Api",
            Description = "Api接口文档"
        }
    };

    /// <summary>
    /// 授权配置
    /// </summary>
    public SimApiAuthOption ApiAuth { get; set; } = new SimApiAuthOption();

    /// <summary>
    /// 文档页面标题
    /// </summary>
    public string DocumentTitle { get; set; } = "API接口文档";

    /// <summary>
    /// 接口支持的调用方式
    /// </summary>
    public SubmitMethod[] SupportedMethod { get; set; } = new[] { SubmitMethod.Post };
}