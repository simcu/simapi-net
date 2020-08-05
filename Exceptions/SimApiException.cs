using System;
namespace SimApi.Exceptions
{
    /// <summary>
    /// Api错误捕获异常
    /// </summary>
    public class SimApiException : Exception
    {
        public int Code { get; }

        public SimApiException(int code, string message = "") : base(message)
        {
            Code = code;
        }
    }
}
