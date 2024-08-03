using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;

namespace SimApi;

public partial class Synapse
{
    public event EventHandler<ConfigStoreItem>? OnConfigChanged;
    private Dictionary<string, string> CurrentConfig { get; } = new();

    private string ConfigStoreTopicPrefix => $"{Options.SysName}/synapse-config-store/";

    private bool FireSetConfig(string key, string value)
    {
        if (key.Contains('#') || key.Contains('+')) return false;
        var topic = $"{Options.SysName}/synapse-config-store/{key}";
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(value)
            .WithRetainFlag()
            .Build();
        if (!Client!.IsConnected) return false;
        Client!.PublishAsync(message, CancellationToken.None).Wait();
        logger.LogDebug("Synapse Config Set: {Config} => {Data}", key, value);
        return true;
    }

    private string? FireGetConfig(string key)
    {
        return CurrentConfig.GetValueOrDefault(key);
    }

    private void RunConfigStoreServer()
    {
        Client!.ApplicationMessageReceivedAsync += e =>
        {
            if (!e.ApplicationMessage.Topic.StartsWith(ConfigStoreTopicPrefix)) return Task.CompletedTask;
            var reqBody = e.ApplicationMessage.ConvertPayloadToString();
            var eventName = e.ApplicationMessage.Topic.Replace(ConfigStoreTopicPrefix, string.Empty);
            CurrentConfig[eventName] = reqBody;
            OnConfigChanged?.Invoke(this, new ConfigStoreItem(eventName, reqBody));
            logger.LogDebug("Synapse Config Changed: {Config} => {Data}", eventName, reqBody);
            return Task.CompletedTask;
        };
        SubConfigStoreServerTopic();
    }

    private void SubConfigStoreServerTopic()
    {
        var csSubOpts = MqttFactory.CreateSubscribeOptionsBuilder()
            .WithTopicFilter(o =>
                o.WithTopic($"{ConfigStoreTopicPrefix}#").WithRetainHandling(MqttRetainHandling.SendAtSubscribe))
            .Build();
        Client!.SubscribeAsync(csSubOpts).Wait();
    }

    public record ConfigStoreItem(string Key, string Value);
}