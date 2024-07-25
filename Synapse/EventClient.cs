using System.Text.Json;
using System.Threading;
using Microsoft.Extensions.Logging;
using MQTTnet;
using SimApi.Helpers;

namespace SimApi;

public partial class Synapse
{
    private bool FireEvent(string eventName, object param, bool retain = false)
    {
        var paramJson = JsonSerializer.Serialize(param, SimApiUtil.JsonOption);
        var topic = $"{Options.SysName}/{Options.AppName}/event/{eventName}";
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(paramJson)
            .WithRetainFlag(retain)
            .Build();
        if (!Client!.IsConnected) return false;
        Client!.PublishAsync(message, CancellationToken.None).Wait();
        logger.LogDebug("Event Publish: {Event}@{App} {Json}", eventName, Options.AppName, paramJson);
        return true;
    }
}