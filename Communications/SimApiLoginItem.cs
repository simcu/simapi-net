using System.Collections.Generic;

namespace SimApi.Communications;

/// <summary>
/// 登录信息中间件
/// </summary>
public class SimApiLoginItem
{
    public string Id { get; set; } = null!;
    public string[] Type { get; set; } = ["user"];
    public Dictionary<string, string> Meta { get; set; } = new();
    public object? Extra { get; set; }
};