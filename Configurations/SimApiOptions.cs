using System;

namespace SimApi.Configs;

public class SimApiOptions
{
    /// <summary>
    /// 启用全部Cors，对于开发前后分离的时候很有用。
    /// default: true
    /// </summary>
    public bool EnableCors { get; set; } = true;

    /// <summary>
    /// 启用SimapiAuth，一个简单的基于Header Token的认证方式。
    /// default: false
    /// </summary>
    public bool EnableSimApiAuth { get; set; } = false;

    /// <summary>
    /// 启用在线文档，启用后 访问 /swagger 可以查看对应的api文档。
    /// default: false
    /// </summary>
    public bool EnableSimApiDoc { get; set; } = false;

    /// <summary>
    /// 启用异常拦截,启用后，所有的异常将被通过json反馈。
    /// default: true
    /// </summary>
    public bool EnableSimApiException { get; set; } = true;

    /// <summary>
    /// 开启S3兼容的存储系统。
    /// default: false
    /// </summary>
    public bool EnableSimApiStorage { get; set; } = false;

    /// <summary>
    /// 开启ForwardHeaders,开启后可以透传负载均衡的Headers
    /// default: true
    /// </summary>
    public bool EnableForwardHeaders { get; set; } = true;

    /// <summary>
    /// 将所有的url都格式化为小写字母
    /// default: true
    /// </summary>
    public bool EnableLowerUrl { get; set; } = true;

    /// <summary>
    /// 启用格式化的 Console Logger
    /// default: false
    /// </summary>
    public bool EnableLogger { get; set; } = false;

    /// <summary>
    /// 是否启用Synapse
    /// </summary>
    public bool EnableSynapse { get; set; } = false;


    /// <summary>
    /// Swagger文档相关配置，需要启用 EnableSimApiDoc
    /// </summary>
    public SimApiDocOptions SimApiDocOptions { get; set; } = new();

    /// <summary>
    /// S3兼容的存储系统配置，需要启用 EnableSimApiStorage
    /// </summary>
    public SimApiStorageOptions SimApiStorageOptions { get; set; } = new();

    public SimApiSynapseOptions SimApiSynapseOptions { get; set; } = new();

    public void ConfigureSimApiSynapse(Action<SimApiSynapseOptions> options = null)
    {
        options?.Invoke(SimApiSynapseOptions);
    }

    public void ConfigureSimApiSynapse(SimApiSynapseOptions options)
    {
        SimApiSynapseOptions = options;
    }

    public void ConfigureSimApiDoc(Action<SimApiDocOptions> options = null)
    {
        options?.Invoke(SimApiDocOptions);
    }

    public void ConfigureSimApiStorage(Action<SimApiStorageOptions> options = null)
    {
        options?.Invoke(SimApiStorageOptions);
    }
}