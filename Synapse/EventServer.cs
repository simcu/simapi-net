using System;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet;
using SimApi.Helpers;

namespace SimApi;

public partial class Synapse
{
    private void RunEventServer()
    {
        var esTopicPrefix = $"{Options.SysName}/event/";
        Client!.ApplicationMessageReceivedAsync += e =>
        {
            if (!e.ApplicationMessage.Topic.StartsWith(esTopicPrefix)) return Task.CompletedTask;
            var reqBody = e.ApplicationMessage.ConvertPayloadToString();
            var eventName = e.ApplicationMessage.Topic.Replace(esTopicPrefix, string.Empty);
            logger.LogDebug("Synapse Event Receive: {EventName}\n{Body}", eventName, reqBody);
            var methods = EventRegistry
                .Where(x => Regex.IsMatch(eventName,
                    "^" + Regex.Escape(x.Key!).Replace("\\+", "[^/]+").Replace("\\#", ".*") + "$"))
                .ToArray();
            foreach (var method in methods)
            {
                var callClass = sp.CreateScope().ServiceProvider.GetRequiredService(method.Class!);
                var mt = callClass.GetType().GetMethod(method.Method!);
                try
                {
                    if (mt!.GetParameters().Length == 2)
                    {
                        var pt = mt.GetParameters()[1].ParameterType;
                        mt.Invoke(callClass, pt == typeof(string)
                            ? [eventName, reqBody]
                            : [eventName, JsonSerializer.Deserialize(reqBody, pt, SimApiUtil.JsonOption)]);
                    }
                    else
                    {
                        mt.Invoke(callClass, [eventName]);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError("Synapse Event Processor Error: {Err}\n{Stack}", ex.Message,ex.StackTrace);
                }
            }

            return Task.CompletedTask;
        };
        foreach (var ev in EventRegistry)
        {
            var topic = $"$queue/{esTopicPrefix}{ev.Key}";
            var evSubOpts = MqttFactory.CreateSubscribeOptionsBuilder()
                .WithTopicFilter(o => o.WithTopic(topic)).Build();
            Client.SubscribeAsync(evSubOpts).Wait();
            logger.LogDebug("Synapse Event Register Event Success: {EventName}\nFull Topic: {Topic}", ev.Key, topic);
        }
    }
}