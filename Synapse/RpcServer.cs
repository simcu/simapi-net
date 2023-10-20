using System;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Events;
using SimApi.Communications;
using SimApi.Exceptions;
using SimApi.Helpers;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace SimApi;

public partial class Synapse
{
    private void RunRpcServer()
    {
        RpcServerChannel = CreateChannel(Options.RpcProcessorNum, "RpcServer");
        var queue = $"{Options.SysName}_{Options.AppName}_server";
        var router = $"server.{Options.AppName}";
        RpcServerChannel.QueueDeclare(queue, true, false, true, null);
        RpcServerChannel.QueueBind(queue, Options.SysName, router, null);
        var consumer = new EventingBasicConsumer(RpcServerChannel);
        consumer.Received += (ch, ea) =>
        {
            var reqBody = Encoding.UTF8.GetString(ea.Body.ToArray());
            Logger.LogDebug(
                "RPC Receive: ({BasicPropertiesMessageId}) {BasicPropertiesReplyTo} -> {BasicPropertiesType}@{OptionsAppName}\n{S}",
                ea.BasicProperties.MessageId, ea.BasicProperties.ReplyTo, ea.BasicProperties.Type, Options.AppName,
                reqBody);
            var res = new SimApiBaseResponse(404, "method not found");
            var method = RpcRegistry.FirstOrDefault(x => x.Key == ea.BasicProperties.Type);
            if (method != null)
            {
                var callClass = Sp.CreateScope().ServiceProvider.GetRequiredService(method.Class);
                var mt = callClass.GetType().GetMethod(method.Method);
                try
                {
                    var pt = mt.GetParameters()[0].ParameterType;
                    if (pt == typeof(string))
                    {
                        var ret = mt.Invoke(callClass, new[] { reqBody });
                        res = new SimApiBaseResponse<object>(ret);
                    }
                    else
                    {
                        var paramObj = JsonSerializer.Deserialize(reqBody, pt, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });
                        var ret = mt.Invoke(callClass, new[] { paramObj });
                        res = new SimApiBaseResponse<object>(ret);
                    }
                }
                catch (SimApiException e)
                {
                    Logger.LogDebug("RPC错误: {Err}", e.Message);
                    res = new SimApiBaseResponse(e.Code, e.Message);
                }
                catch (Exception e)
                {
                    Logger.LogDebug("RPC调用失败: {Err}", e.Message);
                    res = new SimApiBaseResponse(500, e.Message);
                }
            }


            var returnJson = JsonSerializer.Serialize((object)res, new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });
            Console.WriteLine(returnJson);
            var reply = $"client.{ea.BasicProperties.ReplyTo}.{ea.BasicProperties.AppId}";
            var props = RpcServerChannel.CreateBasicProperties();
            props.AppId = Options.AppId;
            props.CorrelationId = ea.BasicProperties.MessageId;
            props.MessageId = Guid.NewGuid().ToString();
            props.ReplyTo = Options.AppName;
            props.Type = ea.BasicProperties.Type;
            RpcServerChannel.BasicPublish(Options.SysName, reply, false, props, Encoding.UTF8.GetBytes(returnJson));
            Logger.LogDebug(
                "Rpc Return: ({BasicPropertiesMessageId}) {BasicPropertiesType}@{OptionsAppName} -> {BasicPropertiesReplyTo}\n{ReturnJson}",
                ea.BasicProperties.MessageId, ea.BasicProperties.Type, Options.AppName, ea.BasicProperties.ReplyTo,
                returnJson);
        };
        RpcServerChannel.BasicConsume(queue, true, "", false, false, null, consumer);
    }
}