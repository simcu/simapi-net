namespace SimApi.ModelBinders;

public abstract class SimApiSignProviderBase
{
    /// <summary>
    /// appId字段的名称
    /// </summary>
    public string? AppIdName { get; set; } = "appId";

    /// <summary>
    /// 时间戳的字段名
    /// </summary>
    public string TimestampName { get; set; } = "timestamp";

    /// <summary>
    /// 随机字符串的字段名
    /// </summary>
    public string NonceName { get; set; } = "nonce";

    /// <summary>
    /// 签名的字段名
    /// </summary>
    public string SignName { get; set; } = "sign";

    /// <summary>
    /// 请求过期时间, 如果为0, 不校验timestamp
    /// </summary>
    public int QueryExpires { get; set; } = 5;

    /// <summary>
    /// 如果开启,必须配置redis, 每次请求将会缓存nonce
    /// </summary>
    public bool DuplicateRequestProtection { get; set; } = true;

    public string[] SignFields { get; set; } = ["appId"];

    /// <summary>
    /// 根据appId获取对应的密钥
    /// </summary>
    /// <param name="appId">应用ID</param>
    /// <returns>密钥（返回null表示获取失败）</returns>
    public abstract string? GetKey(string? appId);
}