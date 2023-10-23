using System;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Microsoft.Extensions.Logging;
using SimApi.Helpers;

namespace SimApi;

public partial class Synapse
{
    private void RunEventClient()
    {
        EventClientChannel = CreateChannel(0, "EventClient");
    }

    private void FireEvent(string eventName, object param)
    {
        var paramJson = JsonSerializer.Serialize(param, SimApiUtil.JsonOption);
        var router = $"event.{Options.AppName}.{eventName}";
        var props = EventClientChannel.CreateBasicProperties();
        props.AppId = Options.AppId;
        props.MessageId = Guid.NewGuid().ToString();
        props.ReplyTo = Options.AppName;
        props.Type = eventName;
        EventClientChannel.BasicPublish(Options.SysName, router, false, props, Encoding.UTF8.GetBytes(paramJson));
        Logger.LogDebug("Event Publish: {OptionsAppName}.{EventName}\n{ParamJson}", Options.AppName, eventName,
            paramJson);
    }
}