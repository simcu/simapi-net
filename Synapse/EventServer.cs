using System;
using System.Collections.Generic;
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
    private string EventServerTopicPrefix => $"{Options.SysName}/event/";

    private void RunEventServer()
    {
        Client!.ApplicationMessageReceivedAsync += async e =>
        {
            if (!e.ApplicationMessage.Topic.StartsWith(EventServerTopicPrefix)) return;
            var reqBody = e.ApplicationMessage.ConvertPayloadToString();
            var eventName = e.ApplicationMessage.Topic.Replace(EventServerTopicPrefix, string.Empty);
            logger.LogDebug("Synapse Event Receive: {EventName}\n{Body}", eventName, reqBody);
            var methods = EventRegistry
                .Where(x => Regex.IsMatch(eventName,
                    "^" + Regex.Escape(x.Key!).Replace("\\+", "[^/]+").Replace("\\#", ".*") + "$"))
                .ToArray();
            var tasks = methods.Select(method => Task.Run(() =>
                {
                    var callClass = sp.CreateScope().ServiceProvider.GetRequiredService(method.Class!);
                    var mt = callClass.GetType().GetMethod(method.Method!);
                    try
                    {
                        if (mt!.GetParameters().Length == 2)
                        {
                            var pt = mt.GetParameters()[1].ParameterType;
                            mt.Invoke(callClass, pt == typeof(string)
                                ? new object?[] { eventName, reqBody }
                                : new object?[] { eventName, JsonSerializer.Deserialize(reqBody, pt, SimApiUtil.JsonOption) });
                        }
                        else
                        {
                            mt.Invoke(callClass, new object[] { eventName });
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError("Synapse Event Processor Error: {Err}\n{Stack}", ex.Message, ex.StackTrace);
                    }
                }))
                .ToList();
            await Task.WhenAll(tasks);
        };
        SubEventServerTopic();
    }

    private void SubEventServerTopic()
    {
        foreach (var ev in EventRegistry)
        {
            var topic = $"{EventServerTopicPrefix}{ev.Key}";
            if (Options.EventLoadBalancing)
            {
                topic = "$queue/" + topic;
            }

            var evSubOpts = MqttFactory.CreateSubscribeOptionsBuilder()
                .WithTopicFilter(o => o.WithTopic(topic)).Build();
            Client!.SubscribeAsync(evSubOpts).Wait();
            logger.LogDebug("Synapse Event Register Event Success: {EventName}\nFull Topic: {Topic}", ev.Key, topic);
        }
    }
}