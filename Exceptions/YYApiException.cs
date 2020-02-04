using System;
namespace YYApi.Exceptions
{
    /// <summary>
    /// Api错误捕获异常
    /// </summary>
    public class YYApiException : Exception
    {
        public int Code { get; }

        public YYApiException(int code, string message = "") : base(message)
        {
            Code = code;
        }
    }
}
