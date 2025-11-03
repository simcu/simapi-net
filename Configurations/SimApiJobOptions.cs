namespace SimApi.Configurations;

public class SimApiJobOptions
{
    /// <summary>
    /// WebUi地址,设置为null表示不启用
    /// 默认 /jobs
    /// </summary>
    public string? DashboardUrl { get; set; } = "/jobs";

    /// <summary>
    /// webui 用户
    /// 默认 admin
    /// </summary>
    public string DashboardAuthUser { get; set; } = "admin";

    /// <summary>
    /// webui 密码
    /// 默认 Admin@123!
    /// </summary>
    public string DashboardAuthPass { get; set; } = "Admin@123!";

    /// <summary>
    /// 设置为null 使用默认redis配置
    /// </summary>
    public string? RedisConfiguration { get; set; }

    /// <summary>
    /// 设置为null 使用默认redis配置
    /// </summary>
    public int? Database { get; set; } = null;
    public SimApiJobServerConfig[] Servers { get; set; } = [new()];
}

public class SimApiJobServerConfig()
{
    public string[] Queues { get; set; } = ["default"];
    public int WorkerNum { get; set; } = 50;
}