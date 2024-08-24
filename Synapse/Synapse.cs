using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Formatter;
using SimApi.Attributes;
using SimApi.Communications;
using SimApi.Configurations;
using SimApi.Exceptions;
using SimApi.Helpers;

namespace SimApi;

public partial class Synapse(SimApiOptions simApiOptions, ILogger<Synapse> logger, IServiceProvider sp)
{
    private SimApiSynapseOptions Options { get; } = simApiOptions.SimApiSynapseOptions;

    private MqttFactory MqttFactory { get; } = new();
    public IMqttClient? Client { get; set; }

    private List<RegisterItem> EventRegistry { get; set; } = new();

    private List<RegisterItem> RpcRegistry { get; set; } = new();

    public void Init()
    {
        logger.LogDebug("Synapse初始化配置信息: {Json}", SimApiUtil.Json(Options));
        if (string.IsNullOrEmpty(Options.AppName) || string.IsNullOrEmpty(Options.SysName))
        {
            logger.LogCritical("Synapse初始化失败:  AppName 和 SysName 不能为空");
        }

        Options.AppId ??= Guid.NewGuid().ToString();
        logger.LogInformation("Synapse Sys Name: {SysName}\nSynapse App Name: {AppName}\nSynapse App Id: {AppId}",
            Options.SysName, Options.AppName, Options.AppId);
        CreateConnection();
        ProcessAttribute();
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

        if (Options.EnableConfigStore)
        {
            RunConfigStoreServer();
            logger.LogInformation("Synapse Config Store Ready [{SysName}] ...", Options.SysName);
        }
    }


    /// <summary>
    /// 调用RPC使用明确的返回值类型
    /// </summary>
    /// <param name="appName"></param>
    /// <param name="method"></param>
    /// <param name="param"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public SimApiBaseResponse<T> Rpc<T>(string appName, string method, dynamic? param = null)
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

        return (res as SimApiBaseResponse<T>)!;
    }

    /// <summary>
    /// 调用RPC使用object作为返回值类型
    /// </summary>
    /// <param name="appName"></param>
    /// <param name="method"></param>
    /// <param name="param"></param>
    /// <returns></returns>
    public SimApiBaseResponse<object> Rpc(string appName, string method, dynamic? param = null)
    {
        return Rpc<object>(appName, method, param);
    }


    /// <summary>
    /// 只能在Rpc方法中使用,快捷抛出异常返回
    /// </summary>
    /// <param name="code"></param>
    /// <param name="message"></param>
    /// <exception cref="SimApiException"></exception>
    public void RpcError(int code, string message = "")
    {
        throw new SimApiException(code, message);
    }

    /// <summary>
    /// 如果条件成立,则爆出错误
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="code"></param>
    /// <param name="message"></param>
    public void RpcErrorWhen(bool condition, int code, string message = "")
    {
        if (condition) RpcError(code, message);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="param"></param>
    public bool Event(string eventName, dynamic? param = null)
    {
        if (!Options.DisableEventClient) return FireEvent(eventName, param);
        logger.LogError("Synapse Event Client Disabled!");
        return false;
    }

    /// <summary>
    /// 设置一个系统配置项
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool SetConfig(string key, string value)
    {
        if (Options.EnableConfigStore) return FireSetConfig(key, value);
        logger.LogError("Synapse Config Store Disabled!");
        return false;
    }

    /// <summary>
    /// 读取一个配置项,如果没有则为空
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public string? GetConfig(string key)
    {
        if (Options.EnableConfigStore) return FireGetConfig(key);
        logger.LogError("Synapse Config Store Disabled!");
        return null;
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
            //RPC客户端
            if (!Options.DisableRpcClient) SubRpcClientTopic();
            if (RpcRegistry.Count > 0) SubRpcServerTopic();
            if (EventRegistry.Count > 0) SubEventServerTopic();
            if (Options.EnableConfigStore) SubConfigStoreServerTopic();
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
                        (SynapseEventAttribute)Attribute.GetCustomAttribute(method, typeof(SynapseEventAttribute))!;
                    var tmp = new RegisterItem
                    {
                        Key = attribute.Name ?? method.Name,
                        Class = type,
                        Method = method.Name
                    };
                    if (tmp.Key.StartsWith('/') || tmp.Key.EndsWith('/'))
                    {
                        logger.LogError("Synapse Event Register Error: {Key} Can't start or end of '/'", tmp.Key);
                        continue;
                    }

                    var callClass = sp.CreateScope().ServiceProvider.GetRequiredService(tmp.Class!);
                    var mt = callClass.GetType().GetMethod(tmp.Method);
                    if (mt!.GetParameters().Length > 2 || mt.GetParameters().Length < 1)
                    {
                        logger.LogError(
                            "Synapse Event Register Error: Only one or two parameter supported. {Key} -> {Method}@{Class}",
                            tmp.Key, tmp.Method, tmp.Class.Name);
                        continue;
                    }

                    EventRegistry.Add(tmp);
                }

                if (method.IsDefined(typeof(SynapseRpcAttribute), false))
                {
                    var attribute =
                        (SynapseRpcAttribute)Attribute.GetCustomAttribute(method, typeof(SynapseRpcAttribute))!;
                    var tmp = new RegisterItem
                    {
                        Key = attribute.Name ?? $"{type.Name}.{method.Name}",
                        Class = type,
                        Method = method.Name
                    };
                    if (tmp.Key.Contains('/') || tmp.Key.Contains('#') || tmp.Key.Contains('+'))
                    {
                        logger.LogError("Synapse Rpc Register Error: {Key} contains '/' , '#' , '+'", tmp.Key);
                        continue;
                    }

                    var callClass = sp.CreateScope().ServiceProvider.GetRequiredService(tmp.Class!);
                    var mt = callClass.GetType().GetMethod(tmp.Method);
                    if (mt!.GetParameters().Length > 1)
                    {
                        logger.LogError(
                            "Synapse Rpc Register Error: Only one or none parameter supported. {Key} -> {Method}@{Class}",
                            tmp.Key, tmp.Method, tmp.Class.Name);
                        continue;
                    }

                    if (RpcRegistry.Any(x => x.Key == tmp.Key))
                    {
                        logger.LogError("Synapse Rpc Register Error: {Key} Already Exists -> {Method}@{Class}", tmp.Key,
                            tmp.Method, tmp.Class.Name);
                        continue;
                    }

                    RpcRegistry.Add(tmp);
                }
            }
        }

        var events = EventRegistry.Aggregate(string.Empty,
            (current, ev) => current + $"\n |- {ev.Key} -> {ev.Method}@{ev.Class!.Name}");
        var rpcList = RpcRegistry.Aggregate(string.Empty,
            (current, ev) => current + $"\n |- {ev.Key} -> {ev.Method}@{ev.Class!.Name}");
        if (EventRegistry.Count > 0) logger.LogInformation(" >> Synapse System 读取Event方法:{Event}", events);
        if (RpcRegistry.Count > 0) logger.LogInformation(" >> Synapse System 读取RPC方法:{Rpc}", rpcList);
    }
}

public class RegisterItem
{
    public string? Key { get; init; }

    public Type? Class { get; init; }

    public string? Method { get; init; }
}