using System;

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
    /// 启用SimApi网关授权, 基于上层网关透传的身份令牌验证
    /// </summary>
    public bool EnableSimApiAuthGate { get; set; }

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
    /// 启用格式化的 Console Logger
    /// default: false
    /// </summary>
    public bool EnableLogger { get; set; } = true;

    /// <summary>
    /// 是否启用SimApiHttpClient
    /// </summary>
    public bool EnableSimApiHttpClient { get; set; } = false;

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

    public SimApiAuthGateOptions SimApiAuthGateOptions { get; set; } = new();

    public SimApiHttpClientOptions SimApiHttpClientOptions { get; set; } = new();

    public SimApiExceptionOptions SimApiExceptionOptions { get; set; } = new();

    public SimApiRouteOptions SimApiRouteOptions { get; set; } = new();

    public void ConfigureSimApiRoute(Action<SimApiRouteOptions>? options = null)
    {
        options?.Invoke(SimApiRouteOptions);
    }

    public void ConfigureSimApiException(Action<SimApiExceptionOptions>? options = null)
    {
        options?.Invoke(SimApiExceptionOptions);
    }

    public void ConfigureSimApiHttpClient(Action<SimApiHttpClientOptions>? options = null)
    {
        options?.Invoke(SimApiHttpClientOptions);
    }

    public void ConfigureSimApiSynapse(Action<SimApiSynapseOptions>? options = null)
    {
        options?.Invoke(SimApiSynapseOptions);
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

    public void ConfigureSimApiAuthGate(Action<SimApiAuthGateOptions>? options = null)
    {
        options?.Invoke(SimApiAuthGateOptions);
    }
}