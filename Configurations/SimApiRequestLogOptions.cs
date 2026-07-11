namespace SimApi.Configurations;

public class SimApiRequestLogOptions
{
    /// <summary>
    /// 是否打印完整的请求Header
    /// </summary>
    public bool ShowFullHeader { get; set; }

    /// <summary>
    /// 是否打印完整的响应体
    /// </summary>
    public bool ShowFullResponse { get; set; }
}
