using System.Collections.Generic;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace SimApi.Configurations;

/// <summary>
/// 文档组配置
/// </summary>
public class SimApiDocGroupOption(string id, string name, string description = "", bool isDefault = false)
{
    /// <summary>
    /// 文档标识
    /// </summary>
    public string Id { get; set; } = id;

    /// <summary>
    /// 文档名称
    /// </summary>
    public string Name { get; set; } = name;

    /// <summary>
    /// 文档描述
    /// </summary>
    public string Description { get; set; } = description!;

    /// <summary>
    /// 是否为默认文档组: 未标注分组的接口全部归入此文档组
    /// </summary>
    public bool IsDefault { get; set; } = isDefault;
}

/// <summary>
/// 授权配置, Type支持 "SimApiAuth","ClientCredentials","Implicit","AuthorizationCode"
/// </summary>
public class SimApiAuthOption
{
    public string[] Type { get; set; } = ["SimApiAuth"];

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
    public SimApiDocGroupOption[] ApiGroups { get; set; } =
    [
        new("api", "Api", "Api接口文档", isDefault: true)
    ];

    /// <summary>
    /// 授权配置
    /// </summary>
    public SimApiAuthOption ApiAuth { get; set; } = new();

    /// <summary>
    /// 接口文档页面访问前缀, 默认 "docs"。
    /// 访问 /docs 即打开文档页面, JSON 地址为 /docs/{文档组Id}.json
    /// </summary>
    public string UrlPrefix { get; set; } = "docs";

    /// <summary>
    /// 文档页面标题
    /// </summary>
    public string DocumentTitle { get; set; } = "API接口文档";

    /// <summary>
    /// 接口支持的调用方式
    /// </summary>
    public SubmitMethod[] SupportedMethod { get; set; } = [SubmitMethod.Post];
}