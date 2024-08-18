using System.Collections.Generic;

namespace SimApi.Communications;

/// <summary>
/// 登录信息中间件
/// </summary>
public class SimApiLoginItem
{
    public string? Id { get; set; }
    public string[] Type { get; set; } = new[] { "user" };
    public Dictionary<string, string>? Meta { get; set; } = null;
    public object? Extra { get; set; } = null;
};