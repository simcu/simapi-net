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

    public bool EnableConfigStore { get; set; } = true;
    public bool DisableEventClient { get; set; } = false;
    public bool DisableRpcClient { get; set; } = false;
}