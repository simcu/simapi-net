namespace SimApi.Configurations;

public class SimApiAuthGateOptions
{
    public string? Server { get; set; }
    public string? AppId { get; set; }
    public string? AppKey { get; set; }

    /// <summary>
    /// 开启则使用内部网关透传的Middleware
    /// 注意: 只有内部应用需要开启这个,也就是api通过内部网关代理后
    /// </summary>
    public bool UseMiddleware { get; set; }
}