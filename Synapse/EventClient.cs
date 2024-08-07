using System.Text.Json;
using System.Threading;
using Microsoft.Extensions.Logging;
using MQTTnet;
using SimApi.Helpers;

namespace SimApi;

public partial class Synapse
{
    private bool FireEvent(string eventName, object? param)
    {
        string paramJson;
        if (param is string strParam)
        {
            paramJson = strParam;
        }
        else
        {
            paramJson  = JsonSerializer.Serialize(param, SimApiUtil.JsonOption);
        }
        var topic = $"{Options.SysName}/event/{Options.AppName}/{eventName}";
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(paramJson)
            .WithRetainFlag(false)
            .Build();
        if (!Client!.IsConnected) return false;
        Client.PublishAsync(message, CancellationToken.None).Wait();
        logger.LogDebug("Synapse Event Publish: {Event}@{App} {Json}", eventName, Options.AppName, paramJson);
        return true;
    }
}