using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;
using SimApi.Helpers;

namespace SimApi;

public partial class Synapse
{
    private void RunEventServer()
    {
        var esTopicPrefix = $"{Options.SysName}/{Options.AppName}/event/";
        var eventSubOpts = MqttFactory.CreateSubscribeOptionsBuilder()
            .WithTopicFilter(o =>
                o.WithTopic($"$queue/{esTopicPrefix}+").WithRetainHandling(MqttRetainHandling.SendAtSubscribe))
            .Build();
        Client.ApplicationMessageReceivedAsync += e =>
        {
            if (!e.ApplicationMessage.Topic.StartsWith(esTopicPrefix)) return Task.CompletedTask;
            var reqBody = e.ApplicationMessage.ConvertPayloadToString();
            var eventName = e.ApplicationMessage.Topic.Replace(esTopicPrefix, string.Empty);
            logger.LogDebug("Synapse Event Receive: {AppName}.{EventName}\n{Body}",
                Options.AppName, eventName, reqBody);

            var method = EventRegistry.FirstOrDefault(x => x.Key == eventName);
            if (method == null) return Task.CompletedTask;
            var callClass = Sp.CreateScope().ServiceProvider.GetRequiredService(method!.Class);
            var mt = callClass.GetType().GetMethod(method.Method);
            var pt = mt!.GetParameters()[0].ParameterType;
            try
            {
                mt.Invoke(callClass, pt == typeof(string)
                    ? new object[] { reqBody }
                    : new[] { JsonSerializer.Deserialize(reqBody, pt, SimApiUtil.JsonOption) });
            }
            catch (Exception ex)
            {
                logger.LogError("SynapseEvent Processor Error: {Err}", ex.InnerException);
            }
            return Task.CompletedTask;
        };
        Client.SubscribeAsync(eventSubOpts).Wait();
    }
}