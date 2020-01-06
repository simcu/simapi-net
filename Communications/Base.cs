using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace YYApi.Communications
{
    /// <summary>
    /// 基础相应
    /// </summary>
    public class BaseResponse
    {
        /// <summary>
        /// 错误代码
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// 错误描述
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 默认错误代码对应提示信息
        /// </summary>
        private readonly Dictionary<int, string> MsgBox = new Dictionary<int, string>()
        {
            {200, "成功"},
            {400, "参数错误"},
            {401, "需要登录"},
            {403, "无权访问"},
            {404, "接口不存在"},
            {500, "服务器错误"}
        };

        /// <summary>
        /// 返回一个成功的空结果
        /// </summary>
        public BaseResponse()
        {
            SetCode(200);
        }


        /// <summary>
        /// 返回指定代码的描述
        /// </summary>
        /// <param name="code">错误代码</param>
        public BaseResponse(int code)
        {
            SetCode(code);
        }

        /// <summary>
        /// 返回指定代码的结果,并自定义提示信息
        /// </summary>
        /// <param name="code">错误代码</param>
        /// <param name="message">错误信息</param>
        public BaseResponse(int code, string message)
        {
            SetCodeMsg(code, message);
        }

        /// <summary>
        /// 设置响应结果的错误代码
        /// </summary>
        /// <param name="code">错误代码</param>
        public void SetCode(int code)
        {
            Code = code;
            Message = MsgBox.ContainsKey(code) ? MsgBox[code] : "未知错误";
        }

        /// <summary>
        /// 设置响应结果的错误代码和错误描述
        /// </summary>
        /// <param name="code">错误代码</param>
        /// <param name="message">错误描述</param>
        public void SetCodeMsg(int code, string message)
        {
            SetCode(code);
            if (!string.IsNullOrEmpty(message))
            {
                Message = message;
            }
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
                Converters = {new JsonStringEnumConverter()}
            });
        }
    }

    /// <summary>
    /// 动态内容
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BaseResponse<T> : BaseResponse
    {
        /// <summary>
        /// 动态内容
        /// </summary>
        public T Data { get; set; }
    }
}