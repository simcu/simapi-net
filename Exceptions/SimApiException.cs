using System;

namespace SimApi.Exceptions;

/// <summary>
/// Api错误捕获异常
/// </summary>
public class SimApiException(int code, string message = "") : Exception(message)
{
    public int Code { get; } = code;
}