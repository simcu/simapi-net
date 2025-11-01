using System;
using SimApi.CoceSdk;

namespace SimApi.Configurations;

public class SimApiOptions
{
    public string? RedisConfiguration { get; set; }

    /// <summary>
    /// 是否启用后台任务系统 *基于Hangfire
    /// </summary>
    public bool EnableJob { get; set; }

    /// <summary>
    /// 启用SimApiAuth，一个简单的基于Header Token的认证方式。
    /// default: false
    /// </summary>
    public bool EnableSimApiAuth { get; set; }

    /// <summary>
    /// 是否使用CoceSdk
    /// </summary>
    public bool EnableCoceSdk { get; set; }

    /// <summary>
    /// 开启S3兼容的存储系统。
    /// default: false
    /// </summary>
    public bool EnableSimApiStorage { get; set; }

    /// <summary>
    /// 启用在线文档，启用后 访问 /swagger 可以查看对应的api文档。
    /// default: false
    /// </summary>
    public bool EnableSimApiDoc { get; set; }

    /// <summary>
    /// 是否启用Synapse
    /// </summary>
    public bool EnableSynapse { get; set; }

    /// <summary>
    /// 启用全部Cors，对于开发前后分离的时候很有用。
    /// default: true
    /// </summary>
    public bool EnableCors { get; set; } = true;


    public CoceAppSdkOption CoceSdkOptions { get; set; } = new();

    /// <summary>
    /// 启用异常拦截,启用后，所有的异常将被通过json反馈。
    /// default: true
    /// </summary>
    public bool EnableSimApiException { get; set; } = true;

    /// <summary>
    /// 启用返回结果拦截
    /// </summary>
    public bool EnableSimApiResponseFilter { get; set; } = true;


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
    /// 应用可以通过 /versions 显示出应用版本和SimApi包版本
    /// default: true
    /// </summary>
    public bool EnableVersionUrl { get; set; } = true;



    /// <summary>
    /// 启用格式化的 Console Logger
    /// default: false
    /// </summary>
    public bool EnableLogger { get; set; } = true;


    /// <summary>
    /// 配置Job
    /// </summary>
    public SimApiJobOptions SimApiJobOptions { get; set; } = new();

    /// <summary>
    /// Swagger文档相关配置，需要启用 EnableSimApiDoc
    /// </summary>
    public SimApiDocOptions SimApiDocOptions { get; set; } = new();

    /// <summary>
    /// S3兼容的存储系统配置，需要启用 EnableSimApiStorage
    /// </summary>
    public SimApiStorageOptions SimApiStorageOptions { get; set; } = new();

    public SimApiSynapseOptions SimApiSynapseOptions { get; set; } = new();

    public void ConfigureSimApiSynapse(Action<SimApiSynapseOptions>? options = null)
    {
        options?.Invoke(SimApiSynapseOptions);
    }

    public void ConfigureCoceSdk(Action<CoceAppSdkOption>? options = null)
    {
        options?.Invoke(CoceSdkOptions);
    }

    public void ConfigureSimApiDoc(Action<SimApiDocOptions>? options = null)
    {
        options?.Invoke(SimApiDocOptions);
    }

    public void ConfigureSimApiStorage(Action<SimApiStorageOptions>? options = null)
    {
        options?.Invoke(SimApiStorageOptions);
    }

    public void ConfigureSimApiJob(Action<SimApiJobOptions>? options = null)
    {
        options?.Invoke(SimApiJobOptions);
    }
}