using System.Collections.Generic;

namespace SimApi.Communications;

/// <summary>
/// 登录信息中间件
/// </summary>
public class SimApiLoginItem
{
    public required string Id { get; set; }
    public string[] Type { get; set; } = ["user"];
    public Dictionary<string, string> Meta { get; set; } = [];
    public object? Extra { get; set; }
};