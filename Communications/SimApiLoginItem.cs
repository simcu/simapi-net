using System.Collections.Generic;

namespace SimApi.Communications;

/// <summary>
/// 登录信息中间件
/// </summary>
public record SimApiLoginItem(string Id, string[] Type,Dictionary<string,string> Meta = null);