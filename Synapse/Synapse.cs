using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using SimApi.Attributes;
using SimApi.Communications;
using SimApi.Configs;
using SimApi.Helpers;

namespace SimApi;

public partial class Synapse
{
    private IServiceProvider Sp { get; }

    private SimApiSynapseOptions Options { get; }

    private ILogger<Synapse> Logger { get; }

    private IConnection Connection { get; set; }

    private IModel EventClientChannel { get; set; }

    private IModel EventServerChannel { get; set; }

    private IModel RpcClientChannel { get; set; }

    private IModel RpcServerChannel { get; set; }

    private List<RegisterItem> EventRegistry { get; set; }

    private List<RegisterItem> RpcRegistry { get; set; }

    public Synapse(SimApiOptions simApiOptions, ILogger<Synapse> logger, IServiceProvider sp)
    {
        Logger = logger;
        Sp = sp;
        Options = simApiOptions.SimApiSynapseOptions;
    }

    public void Init()
    {
        ProcessAttribute();
        Logger.LogDebug("Synapse初始化配置信息: {Json}", SimApiUtil.Json(Options));
        if (string.IsNullOrEmpty(Options.AppName) || string.IsNullOrEmpty(Options.SysName))
        {
            Logger.LogCritical("Synapse初始化失败: AppName or SysName 错误");
        }
        Options.AppId ??= Guid.NewGuid().ToString();
        Logger.LogInformation("System Name: {SysName}\nApp Name: {AppName}\nAppId: {AppId}", Options.SysName,
            Options.AppName, Options.AppId);
        CreateConnection();
        CheckAndCreateExchange();
        //事件客户端
        if (Options.DisableEventClient)
        {
            Logger.LogWarning("Event Client Disabled: DisableEventClient set true");
        }
        else
        {
            RunEventClient();
            Logger.LogInformation("Event Client Ready");
        }

        //RPC客户端
        if (Options.DisableRpcClient)
        {
            Logger.LogWarning("Rpc Client Disabled: DisableEventClient set true");
        }
        else
        {
            RunRpcClient();
            Logger.LogInformation("Rpc Client Ready, Client Timeout: {OptionsRpcTimeout}s", Options.RpcTimeout);
        }
        if (RpcRegistry.Count > 0)
        {
            RunRpcServer();
        }
        if (EventRegistry.Count > 0)
        {
            RunEventServer();
        }
    }


    public SimApiBaseResponse<T> Rpc<T>(string appName, string method, dynamic param)
    {
        var res = new SimApiBaseResponse(500, "Rpc Client Disabled!");
        if (Options.DisableRpcClient)
        {
            Logger.LogError("Rpc Client Disabled!");
        }
        else
        {
            var data = FireRpc(appName, method, param);
            res = JsonSerializer.Deserialize<SimApiBaseResponse<T>>(data, SimApiUtil.JsonOption);
        }
        return res as SimApiBaseResponse<T>;
    }

    public SimApiBaseResponse<object> Rpc(string appName, string method, dynamic param)
    {
        return Rpc<object>(appName, method, param);
    }

    public void Event(string eventName, dynamic param)
    {
        if (Options.DisableEventClient)
        {
            Logger.LogError("Event Client Disabled!");
        }
        else
        {
            FireEvent(eventName, param);
        }
    }

    private void CreateConnection()
    {
        var factory = new ConnectionFactory
        {
            HostName = Options.MqHost,
            Port = Options.MqPort,
            VirtualHost = Options.MqVHost,
            UserName = Options.MqUser,
            Password = Options.MqPass
        };
        try
        {
            Connection = factory.CreateConnection();
            Logger.LogInformation("连接RabbitMQ服务器成功");
        }
        catch (BrokerUnreachableException e)
        {
            Logger.LogError("连接RabbitMQ失败: \n{Err}", e);
        }
    }

    private IModel CreateChannel(ushort processNum = 0, string desc = "unknow")
    {
        IModel channel = null;
        try
        {
            var log = $"Channel [{desc}] 创建成功...";
            channel = Connection.CreateModel();
            if (processNum != 0)
            {
                channel.BasicQos(0, processNum, false);
                log += $"最大处理器数量: {processNum}";
            }
            Logger.LogInformation(log);
        }
        catch (ConnectFailureException e)
        {
            Logger.LogError("Channel [{{Desc}}] 创建失败...\n {0}", e);
        }
        return channel;
    }

    private void CheckAndCreateExchange()
    {
        var channel = CreateChannel(0, "Exchange");
        try
        {
            channel.ExchangeDeclare(Options.SysName, ExchangeType.Topic, true, true, null);
            Logger.LogDebug("Register Exchange Success");
        }
        catch (ConnectFailureException e)
        {
            Logger.LogError("Failed to declare Exchange.\n {Err}", e);
        }
        channel.Close();
        Logger.LogDebug("Exchange Channel Closed");
    }

    private void ProcessAttribute()
    {
        EventRegistry = new List<RegisterItem>();
        RpcRegistry = new List<RegisterItem>();
        var stackTrace = new StackTrace();
        var callingMethod = stackTrace.GetFrame(stackTrace.FrameCount - 1).GetMethod();
        var assembly = callingMethod.DeclaringType.Assembly;
        var types = assembly.GetTypes(); // 获取程序集中的所有类型
        foreach (var type in types)
        {
            var methods = type.GetMethods(); // 获取类型中的所有方法
            foreach (var method in methods)
            {
                if (method.IsDefined(typeof(SynapseEventAttribute), false))
                {
                    var attribute =
                        (SynapseEventAttribute)Attribute.GetCustomAttribute(method, typeof(SynapseEventAttribute));
                    if (attribute != null)
                    {
                        EventRegistry.Add(new RegisterItem
                        {
                            Key = attribute.Name ?? method.Name,
                            Class = type,
                            Method = method.Name
                        });
                    }
                }
                if (method.IsDefined(typeof(SynapseRpcAttribute), false))
                {
                    var attribute =
                        (SynapseRpcAttribute)Attribute.GetCustomAttribute(method, typeof(SynapseRpcAttribute));
                    if (attribute != null)
                    {
                        RpcRegistry.Add(new RegisterItem
                        {
                            Key = attribute.Name ?? $"{type.Name}.{method.Name}",
                            Class = type,
                            Method = method.Name
                        });
                    }
                }
            }
        }
        var events = EventRegistry.Aggregate(string.Empty,
            (current, ev) => current + $"\n |- {ev.Key} -> {ev.Method}@{ev.Class.Name}");
        var rpcList = RpcRegistry.Aggregate(string.Empty,
            (current, ev) => current + $"\n |- {ev.Key} -> {ev.Method}@{ev.Class.Name}");
        Logger.LogInformation(">>> Synapse System 读取Event方法:{Event}\n>>>Synapse System 读取RPC方法:{Rpc}", events, rpcList);
    }
}

public class RegisterItem
{
    public string Key { get; set; }

    public Type Class { get; set; }

    public string Method { get; set; }
}