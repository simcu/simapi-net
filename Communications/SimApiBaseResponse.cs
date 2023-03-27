using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimApi.Communications
{
    /// <summary>
    /// 基础相应
    /// </summary>
    public record SimApiBaseResponse(int Code = 200, string Message = "成功")
    {
        /// <summary>
        /// 默认错误代码对应提示信息
        /// </summary>
        private static readonly Dictionary<int, string> MsgBox = new()
        {
            {
                200, "成功"
            },
            {
                204, "没有数据"
            },
            {
                400, "参数错误"
            },
            {
                401, "需要登录"
            },
            {
                403, "无权访问"
            },
            {
                404, "接口不存在"
            },
            {
                500, "服务器错误"
            }
        };

        public SimApiBaseResponse(int code) : this(code, MsgBox.ContainsKey(code) ? MsgBox[code] : "未知错误")
        {
        }

        /// <summary>
        /// 序列化为JSON字符串
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                IgnoreReadOnlyProperties = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters =
                {
                    new JsonStringEnumConverter()
                }
            });
        }
    }

    /// <summary>
    /// 动态内容分页
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public record SimApiBasePageResponse<T>
    (T List, int Page = 1, int Count = 1, int Total = 1, int Code = 200,
        string Message = "成功") : SimApiBaseResponse(Code, Message);

    /// <summary>
    /// 动态Data返回
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public record SimApiBaseResponse<T>(T Data, int Code = 200, string Message = "成功") : SimApiBaseResponse(Code,
        Message);
}