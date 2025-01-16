namespace SimApi.Configurations;

public class SimApiJobOptions
{
    /// <summary>
    /// WebUi地址,设置为null表示不启用
    /// </summary>
    public string? DashboardUrl { get; set; } = "/jobs";

    public string DashboardAuthUser { get; set; } = "admin";
    public string DashboardAuthPass { get; set; } = "Admin@123!";
    public string? RedisConfiguration { get; set; }

    public int? Database { get; set; } = null;
    public SimApiJobServerConfig[] Servers { get; set; } = [new()];
}

public class SimApiJobServerConfig()
{
    public string[] Queues { get; set; } = ["default"];
    public int WorkerNum { get; set; } = 50;
}