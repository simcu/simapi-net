namespace SimApi.Configs;

public class SimApiSynapseOptions
{
    public string MqHost { get; set; }

    public int MqPort { get; set; }

    public string MqUser { get; set; }

    public string MqPass { get; set; }

    public string MqVHost { get; set; } = "/";

    public string SysName { get; set; }

    public string AppName { get; set; }

    public string AppId { get; set; }

    public int RpcTimeout { get; set; } = 3;

    public ushort EventProcessorNum { get; set; } = 20;

    public ushort RpcProcessorNum { get; set; } = 20;

    public bool DisableEventClient { get; set; } = false;

    public bool DisableRpcClient { get; set; } = false;
}