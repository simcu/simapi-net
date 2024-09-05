using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Packets;
using SimApi.Communications;
using SimApi.Helpers;

namespace SimApi;

public partial class Synapse
{
    private Dictionary<string, TaskCompletionSource<string>> ResponseCompletionSources { get; } = new();

    private string EventClientTopicPrefix => $"{Options.SysName}/{Options.AppName}/rpc/client/{Options.AppId}/";

    private void RunRpcClient()
    {
        Client!.ApplicationMessageReceivedAsync += e =>
        {
            if (!e.ApplicationMessage.Topic.StartsWith(EventClientTopicPrefix)) return Task.CompletedTask;
            var reqBody = e.ApplicationMessage.ConvertPayloadToString();
            var messageId = e.ApplicationMessage.Topic.Replace(EventClientTopicPrefix, string.Empty);
            if (!ResponseCompletionSources.TryGetValue(messageId, out var tcs)) return Task.CompletedTask;
            tcs.SetResult(reqBody);
            ResponseCompletionSources.Remove(messageId);
            return Task.CompletedTask;
        };
        SubRpcClientTopic();
    }

    private void SubRpcClientTopic()
    {
        var rcSubOpts = MqttFactory.CreateSubscribeOptionsBuilder()
            .WithTopicFilter(o => o.WithTopic($"{EventClientTopicPrefix}+")).Build();
        Client!.SubscribeAsync(rcSubOpts).Wait();
    }

    private string? FireRpc(string app, string action, object? param, Dictionary<string, string>? headers = null)
    {
        string paramJson;
        if (param is string strParam)
        {
            paramJson = strParam;
        }
        else
        {
            paramJson = JsonSerializer.Serialize(param, SimApiUtil.JsonOption);
        }

        var topic = $"{Options.SysName}/{app}/rpc/server/{action}";
        var messageId = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource<string>();
        ResponseCompletionSources.Add(messageId, tcs);
        var messageBuilder = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(paramJson)
            .WithResponseTopic($"{Options.AppName},{Options.AppId},{messageId}")
            .WithContentType("application/json")
            .WithRetainFlag(false);
        foreach (var h in headers ?? new Dictionary<string, string>())
        {
            messageBuilder.WithUserProperty(h.Key, h.Value);
        }

        var message = messageBuilder.Build();
        if (!Client!.IsConnected) return null;
        Client.PublishAsync(message, CancellationToken.None).Wait();
        logger.LogDebug(
            "Synapse RPC Client Request: ({PropsMessageId}) {OptionsAppName} -> {Action}@{App}\n{ParamJson}\nHeaders: {Headers}",
            messageId, Options.AppName, action, app, paramJson, JsonSerializer.Serialize(headers));

        string response;
        try
        {
            if (tcs.Task.Wait(Options.RpcTimeout * 1000))
            {
                response = tcs.Task.Result;
                logger.LogDebug(
                    "Synapse RPC Client Response: ({BasicPropertiesCorrelationId}) {BasicPropertiesType}@{BasicPropertiesReplyTo} -> {OptionsAppName}\n{S}",
                    messageId, action, app, Options.AppName, response);
            }
            else
            {
                response = JsonSerializer.Serialize(new SimApiBaseResponse(502, "timeout"), SimApiUtil.JsonOption);
            }
        }
        catch
        {
            response = JsonSerializer.Serialize(new SimApiBaseResponse(500, "Synapse RPC Client Error"),
                SimApiUtil.JsonOption);
        }

        return response;
    }
}