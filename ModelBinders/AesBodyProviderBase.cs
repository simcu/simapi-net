namespace SimApi.ModelBinders;

/// <summary>
/// 密钥提供器接口（抽象密钥获取逻辑）
/// </summary>
public abstract class AesBodyProviderBase
{
    public string? AppIdName { get; set; } = "appId";


    /// <summary>
    /// 根据appId获取对应的密钥
    /// </summary>
    /// <param name="appId">应用ID</param>
    /// <returns>密钥（返回null表示获取失败）</returns>
    public abstract string? GetKey(string? appId);
}