using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;
using SimApi.Communications;
using SimApi.Exceptions;
using SimApi.Helpers;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace SimApi;

public partial class Synapse
{
    private string RpcServerTopicPrefix => $"{Options.SysName}/{Options.AppName}/rpc/server/";

    private void RunRpcServer()
    {
        Client!.ApplicationMessageReceivedAsync += e =>
        {
            if (!e.ApplicationMessage.Topic.StartsWith(RpcServerTopicPrefix)) return Task.CompletedTask;
            var reqBody = e.ApplicationMessage.ConvertPayloadToString();
            var action = e.ApplicationMessage.Topic.Replace(RpcServerTopicPrefix, string.Empty);
            var appInfo = e.ApplicationMessage.ResponseTopic.Split(",");
            logger.LogDebug(
                "Synapse RPC Server Receive: ({BasicPropertiesMessageId}) {BasicPropertiesReplyTo} -> {BasicPropertiesType}@{OptionsAppName}\n{S}",
                e.ApplicationMessage.ContentType, appInfo[0], action, Options.AppName,
                reqBody);
            SimApiBaseResponse res;
            var method = RpcRegistry.FirstOrDefault(x => x.Key == action);
            if (method == null)
            {
                res = new SimApiBaseResponse(404, "method not found");
            }
            else
            {
                var callClass = sp.CreateScope().ServiceProvider.GetRequiredService(method.Class!);
                var mt = callClass.GetType().GetMethod(method.Method!);
                try
                {
                    var methodParams = mt!.GetParameters();
                    object? ret;
                    if (methodParams.Length == 0)
                    {
                        ret = mt.Invoke(callClass, []);
                    }
                    else
                    {
                        var pt = mt.GetParameters()[0].ParameterType;
                        var param = pt == typeof(string)
                            ? [reqBody]
                            : new[] { JsonSerializer.Deserialize(reqBody, pt, SimApiUtil.JsonOption) };
                        ret = mt.Invoke(callClass, param);
                    }

                    res = new SimApiBaseResponse<object?>
                    {
                        Data = ret
                    };
                }
                catch (TargetInvocationException ex)
                {
                    if (ex.InnerException is SimApiException ie)
                    {
                        logger.LogDebug("Synapse RPC调用错误: {Err}", ie.Message);
                        res = new SimApiBaseResponse(ie.Code, ie.Message);
                    }
                    else
                    {
                        res = new SimApiBaseResponse(500, ex.Message);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogDebug("Synapse RPC调用失败: {Err}", ex.Message);
                    res = new SimApiBaseResponse(500, ex.Message);
                }
            }

            var returnJson = JsonSerializer.Serialize((object)res, SimApiUtil.JsonOption);
            var reply = $"{Options.SysName}/{appInfo[0]}/rpc/client/{appInfo[1]}/{e.ApplicationMessage.ContentType}";
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(reply)
                .WithPayload(returnJson)
                .WithRetainFlag(false)
                .Build();
            if (!Client.IsConnected) return Task.CompletedTask;
            Client.PublishAsync(message, CancellationToken.None).Wait();
            logger.LogDebug(
                "Synapse Rpc Server Return: ({BasicPropertiesMessageId}) {BasicPropertiesType}@{OptionsAppName} -> {BasicPropertiesReplyTo}\n{ReturnJson}",
                e.ApplicationMessage.ContentType, action, Options.AppName, appInfo[0], returnJson);

            return Task.CompletedTask;
        };
        SubRpcServerTopic();
    }

    private void SubRpcServerTopic()
    {
        var rsSubOpts = MqttFactory.CreateSubscribeOptionsBuilder()
            .WithTopicFilter(o =>
                o.WithTopic($"$queue/{RpcServerTopicPrefix}+").WithRetainHandling(MqttRetainHandling.SendAtSubscribe))
            .Build();
        Client!.SubscribeAsync(rsSubOpts).Wait();
    }
}