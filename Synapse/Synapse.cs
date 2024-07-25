using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Formatter;
using SimApi.Attributes;
using SimApi.Communications;
using SimApi.Configurations;
using SimApi.Helpers;

namespace SimApi;

public partial class Synapse(SimApiOptions simApiOptions, ILogger<Synapse> logger, IServiceProvider sp)
{
    private IServiceProvider Sp { get; } = sp;

    private SimApiSynapseOptions Options { get; } = simApiOptions.SimApiSynapseOptions;

    private MqttFactory MqttFactory { get; } = new();
    public IMqttClient Client { get; set; }

    private List<RegisterItem> EventRegistry { get; set; }

    private List<RegisterItem> RpcRegistry { get; set; }

    public void Init()
    {
        ProcessAttribute();
        logger.LogDebug("Synapse初始化配置信息: {Json}", SimApiUtil.Json(Options));
        if (string.IsNullOrEmpty(Options.AppName) || string.IsNullOrEmpty(Options.SysName))
        {
            logger.LogCritical("Synapse初始化失败:  AppName 和 SysName 不能为空");
        }

        Options.AppId ??= Guid.NewGuid().ToString();
        logger.LogInformation("Synapse Sys Name: {SysName}\nSynapse App Name: {AppName}\nSynapse App Id: {AppId}",
            Options.SysName, Options.AppName, Options.AppId);
        CreateConnection();
        //事件客户端
        if (Options.DisableEventClient)
        {
            logger.LogWarning("Synapse Event Client Disabled: DisableEventClient set true");
        }
        else
        {
            logger.LogInformation("Synapse Event Client Ready");
        }

        //RPC客户端
        if (Options.DisableRpcClient)
        {
            logger.LogWarning("Synapse Rpc Client Disabled: DisableEventClient set true");
        }
        else
        {
            RunRpcClient();
            logger.LogInformation("Synapse Rpc Client Ready, Client Timeout: {OptionsRpcTimeout}s", Options.RpcTimeout);
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
        var res = new SimApiBaseResponse(500, "Synapse Rpc Client Disabled!");
        if (Options.DisableRpcClient)
        {
            logger.LogError("Synapse Rpc Client Disabled!");
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
            logger.LogError("Synapse Event Client Disabled!");
        }
        else
        {
            FireEvent(eventName, param);
        }
    }

    private void CreateConnection()
    {
        Client = MqttFactory.CreateMqttClient();
        var clientOpts = new MqttClientOptionsBuilder().WithProtocolVersion(MqttProtocolVersion.V500)
            .WithWebSocketServer(o => o.WithUri(Options.Websocket))
            .WithCredentials(Options.Username, Options.Password)
            .WithClientId($"{Options.AppName}:{Options.AppId}")
            .Build();
        Client!.ConnectAsync(clientOpts, CancellationToken.None).Wait();
        Client.ConnectedAsync += _ =>
        {
            logger.LogInformation("Synapse MQTT[{AppName}:{AppId}] 连接成功...", Options.AppName, Options.AppId);
            return Task.CompletedTask;
        };
        //重连
        Client.DisconnectedAsync += async _ =>
        {
            logger.LogError("Synapse MQTT[{AppName}:{AppId}] 断开连接,开始重连...", Options.AppName, Options.AppId);
            await Task.Delay(TimeSpan.FromSeconds(5));
            try
            {
                logger.LogInformation("Synapse MQTT[{AppName}:{AppId}] 开始连接MQTT服务器...", Options.AppName, Options.AppId);
                Client.ConnectAsync(clientOpts).Wait();
            }
            catch
            {
                logger.LogError("Synapse MQTT[{AppName}:{AppId}] 重连失败...", Options.AppName, Options.AppId);
            }
        };
    }

    private void ProcessAttribute()
    {
        EventRegistry = [];
        RpcRegistry = [];
        var stackTrace = new StackTrace();
        var callingMethod = stackTrace.GetFrame(stackTrace.FrameCount - 1)?.GetMethod();
        var assembly = callingMethod?.DeclaringType?.Assembly;
        var types = assembly!.GetTypes(); // 获取程序集中的所有类型
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
        if (EventRegistry.Count > 0) logger.LogInformation(">>> Synapse System 读取Event方法:{Event}", events);
        if (RpcRegistry.Count > 0) logger.LogInformation(">>> Synapse System 读取RPC方法:{Rpc}", rpcList);
    }
}

public class RegisterItem
{
    public string Key { get; init; }

    public Type Class { get; init; }

    public string Method { get; init; }
}