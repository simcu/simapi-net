using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Events;
using SimApi.Communications;
using SimApi.Helpers;

namespace SimApi;

public partial class Synapse
{
    private Dictionary<string, byte[]> ResponseCache { get; } = new();

    private void RunRpcClient()
    {
        RpcClientChannel = CreateChannel(0, "RpcClient");
        var queue = $"{Options.SysName}_{Options.AppName}_client_{Options.AppId}";
        var router = $"client.{Options.AppName}.{Options.AppId}";
        RpcClientChannel.QueueDeclare(queue, true, false, true, null);
        RpcClientChannel.QueueBind(queue, Options.SysName, router, null);
        var consumer = new EventingBasicConsumer(RpcClientChannel);
        consumer.Received += (ch, ea) =>
        {
            ResponseCache.Add(ea.BasicProperties.CorrelationId, ea.Body.ToArray());
            RpcClientChannel.BasicAck(ea.DeliveryTag, false);
            Logger.LogDebug(
                "RPC Response: ({BasicPropertiesCorrelationId}) {BasicPropertiesType}@{BasicPropertiesReplyTo} -> {OptionsAppName}\n{S}",
                ea.BasicProperties.CorrelationId, ea.BasicProperties.Type, ea.BasicProperties.ReplyTo, Options.AppName,
                Encoding.UTF8.GetString(ea.Body.ToArray()));
        };
        RpcClientChannel.BasicConsume(queue, false, "", false, false, null, consumer);
    }

    private object FireRpc(string app, string action, object param)
    {
        var paramJson = JsonSerializer.Serialize(param, new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });
        var router = $"server.{app}";
        SimApiBaseResponse response;
        var props = RpcClientChannel.CreateBasicProperties();
        props.AppId = Options.AppId;
        props.MessageId = Guid.NewGuid().ToString();
        props.Type = action;
        props.ReplyTo = Options.AppName;
        RpcClientChannel.BasicPublish(Options.SysName, router, false, props, Encoding.UTF8.GetBytes(paramJson));
        Logger.LogDebug("RPC Request: ({PropsMessageId}) {OptionsAppName} -> {Action}@{App}\n{ParamJson}",
            props.MessageId,
            Options.AppName, action, app, paramJson);
        var ts = SimApiUtil.TimestampNow;
        while (true)
        {
            if (SimApiUtil.TimestampNow - ts > Options.RpcTimeout)
            {
                response = new SimApiBaseResponse(502, "timeout");
                break;
            }
            if (ResponseCache.TryGetValue(props.MessageId, out var value))
            {
                response = JsonSerializer.Deserialize<SimApiBaseResponse<object>>(
                    Encoding.UTF8.GetString(value));
                ResponseCache.Remove(props.MessageId);
                break;
            }
        }
        return response;
    }
}