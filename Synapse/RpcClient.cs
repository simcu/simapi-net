using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet;
using SimApi.Communications;
using SimApi.Helpers;

namespace SimApi;

public partial class Synapse
{
    private Dictionary<string, string> ResponseCache { get; } = new();

    private void RunRpcClient()
    {
        var rcTopic = $"{Options.SysName}/{Options.AppName}/rpc/client/{Options.AppId}/";
        var rcSubOpts = MqttFactory.CreateSubscribeOptionsBuilder()
            .WithTopicFilter(o => o.WithTopic($"{rcTopic}+")).Build();
        Client.ApplicationMessageReceivedAsync += e =>
        {
            if (!e.ApplicationMessage.Topic.StartsWith(rcTopic)) return Task.CompletedTask;
            var reqBody = e.ApplicationMessage.ConvertPayloadToString();
            var messageId = e.ApplicationMessage.Topic.Replace(rcTopic, string.Empty);
            ResponseCache.Add(messageId, reqBody);
            logger.LogDebug("Synapse RPC Client Message: ({BasicPropertiesCorrelationId}) => {S}", messageId, reqBody);
            return Task.CompletedTask;
        };
        Client.SubscribeAsync(rcSubOpts).Wait();
    }

    private string FireRpc(string app, string action, object param)
    {
        var paramJson = JsonSerializer.Serialize(param, SimApiUtil.JsonOption);
        string response;
        var topic = $"{Options.SysName}/{app}/rpc/server/{action}";
        var messageId = Guid.NewGuid().ToString();
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(paramJson)
            .WithResponseTopic($"{Options.AppName},{Options.AppId}")
            .WithContentType(messageId)
            .WithRetainFlag(false)
            .Build();
        if (!Client!.IsConnected) return null;
        Client!.PublishAsync(message, CancellationToken.None).Wait();
        logger.LogDebug(
            "Synapse RPC Client Request: ({PropsMessageId}) {OptionsAppName} -> {Action}@{App}\n{ParamJson}",
            messageId, Options.AppName, action, app, paramJson);
        var ts = SimApiUtil.TimestampNow;
        while (true)
        {
            if (SimApiUtil.TimestampNow - ts > Options.RpcTimeout)
            {
                response = JsonSerializer.Serialize(new SimApiBaseResponse(502, "timeout"), SimApiUtil.JsonOption);
                break;
            }

            if (!ResponseCache.TryGetValue(messageId, out var value)) continue;
            response = value;
            ResponseCache.Remove(messageId);
            logger.LogDebug(
                "Synapse RPC Client Response: ({BasicPropertiesCorrelationId}) {BasicPropertiesType}@{BasicPropertiesReplyTo} -> {OptionsAppName}\n{S}",
                messageId, action, app, Options.AppName, response);
            break;
        }

        return response;
    }
}