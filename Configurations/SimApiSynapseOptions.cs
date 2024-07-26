namespace SimApi.Configurations;

public class SimApiSynapseOptions
{
    /// <summary>
    /// Mqtt服务器的Websocket地址
    /// </summary>
    public string? Websocket { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? SysName { get; set; }
    public string? AppName { get; set; }
    public string? AppId { get; set; }
    public int RpcTimeout { get; set; } = 3;

    /// <summary>
    /// Event是否使用负载均衡
    /// 也就是订阅$queue主题,消息会分发给不同的AppId
    /// 如果false,多个AppId都可以同时收到消息
    /// </summary>
    public bool EventLoadBalancing { get; set; } = false;
    public bool EnableConfigStore { get; set; } = true;
    public bool DisableEventClient { get; set; } = false;
    public bool DisableRpcClient { get; set; } = false;
}