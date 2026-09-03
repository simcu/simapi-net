using System;
using Swashbuckle.AspNetCore.Annotations;

namespace SimApi.Attributes;

/// <summary>
/// 快捷自定义接口文档类(所有参数均可省略, 支持命名参数)。
/// 对应仓颉版 SimApiDoc:
///   tags        → 接口标签(逗号分隔, 如 "认证,用户")
///   name        → API 名称(映射到 Summary, 作为文档接口标题)
///   description → API 详细描述
///   groupNames  → 所属文档组(逗号分隔, 如 "api,admin"; "*" 表示所有文档; null/空 → 仅默认 "api" 文档)
///   ignore      → true 时不出现在任何文档中(路由不受影响)
/// 写法示例:
///   [SimApiDoc("认证", "登录")]                      位置参数(保持旧版兼容)
///   [SimApiDoc(tags: "认证", name: "登录", description: "...")]
///   [SimApiDoc(groupNames: "api,admin")]             仅指定文档组
///   [SimApiDoc(GroupNames = "*", Ignore = false)]    属性命名参数
///   [SimApiDoc]                                      全部默认(可标在类上, 仅用 Ignore/GroupNames 等)
/// 属性命名参数优先于构造命名参数。
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class SimApiDocAttribute : SwaggerOperationAttribute
{
    /// <summary>
    /// 出现在所有文档组中的通配符
    /// </summary>
    public const string AllGroups = "*";

    /// <summary>
    /// 接口所属的文档组(逗号分隔, 如 "api,admin"); "*" 表示所有文档; null/空 表示未分组(仅默认 "api" 文档)
    /// </summary>
    public string? GroupNames { get; set; }

    /// <summary>
    /// 为 true 时该接口不出现在任何文档中(不影响路由)
    /// </summary>
    public bool Ignore { get; set; }

    /// <summary>
    /// 定义接口说明(全部可选)
    /// </summary>
    /// <param name="tags">接口标签, 逗号分隔, 如 "认证,用户"</param>
    /// <param name="name">接口名称</param>
    /// <param name="description">接口描述</param>
    /// <param name="groupNames">所属文档组, 逗号分隔; "*" 表示所有文档</param>
    /// <param name="ignore">true 时不出现在任何文档</param>
    public SimApiDocAttribute(string? tags = null, string? name = null, string? description = null,
        string? groupNames = null, bool ignore = false)
    {
        Apply(tags, name, description, groupNames, ignore);
    }

    /// <summary>
    /// 定义接口说明(标签以数组传入)
    /// </summary>
    /// <param name="tags">接口标签列表</param>
    /// <param name="name">接口名称</param>
    /// <param name="description">接口描述</param>
    /// <param name="groupNames">所属文档组, 逗号分隔; "*" 表示所有文档</param>
    /// <param name="ignore">true 时不出现在任何文档</param>
    public SimApiDocAttribute(string[] tags, string? name = null, string? description = null,
        string? groupNames = null, bool ignore = false)
    {
        Apply(tags, name, description, groupNames, ignore);
    }

    private void Apply(string? tags, string? name, string? description,
        string? groupNames, bool ignore)
    {
        if (!string.IsNullOrWhiteSpace(tags))
        {
            Tags = tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        ApplyCore(name, description, groupNames, ignore);
    }

    private void Apply(string[] tags, string? name, string? description,
        string? groupNames, bool ignore)
    {
        if (tags is { Length: > 0 })
        {
            Tags = tags;
        }

        ApplyCore(name, description, groupNames, ignore);
    }

    private void ApplyCore(string? name, string? description, string? groupNames, bool ignore)
    {
        if (!string.IsNullOrEmpty(name))
        {
            Summary = name;
        }

        if (!string.IsNullOrEmpty(description))
        {
            Description = description;
        }

        GroupNames = groupNames;
        Ignore = ignore;
    }
}
