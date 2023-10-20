using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Events;

namespace SimApi;

public partial class Synapse
{
    private void RunEventServer()
    {
        EventServerChannel = CreateChannel(Options.EventProcessorNum, "EventServer");
        var queue = $"{Options.SysName}_{Options.AppName}_event";
        EventServerChannel.QueueDeclare(queue, true, false, true, null);
        foreach (var ev in EventRegistry.Where(ev => !ev.Key.Contains('*') && !ev.Key.Contains('#')))
        {
            EventServerChannel.QueueBind(queue, Options.SysName, $"event.{ev.Key}", null);
        }
        var consumer = new EventingBasicConsumer(EventServerChannel);
        consumer.Received += (ch, ea) =>
        {
            var reqBody = Encoding.UTF8.GetString(ea.Body.ToArray());
            Logger.LogDebug("Event Receive: {BasicPropertiesReplyTo}.{BasicPropertiesType}\n{S}",
                ea.BasicProperties.ReplyTo, ea.BasicProperties.Type, reqBody);

            var key = ea.RoutingKey.Replace("event.", string.Empty);
            var method = EventRegistry.FirstOrDefault(x => x.Key == key);
            var callClass = Sp.CreateScope().ServiceProvider.GetRequiredService(method.Class);
            var mt = callClass.GetType().GetMethod(method.Method);
            var pt = mt.GetParameters()[0].ParameterType;
            try
            {
                mt.Invoke(callClass, pt == typeof(string)
                    ? new object[] { reqBody }
                    : new[]
                    {
                        JsonSerializer.Deserialize(reqBody, pt, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        })
                    });
                EventServerChannel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception e)
            {
                Logger.LogError("Event Processor Error: {Err}", e.InnerException);
                EventServerChannel.BasicNack(ea.DeliveryTag, false, false);
            }
        };
        EventServerChannel.BasicConsume(queue, false, "", false, false, null, consumer);
    }
}