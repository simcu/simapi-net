using System.Diagnostics.CodeAnalysis;
using SimApi.Exceptions;

namespace SimApi.Helpers;

public static class SimApiError
{
    /// <summary>
    /// 错误返回
    /// </summary>
    /// <param name="code">错误代码</param>
    /// <param name="message">错误描述(若是常规错误,代码可自动带取描述)</param>
    /// <returns></returns>
    public static void Error(int code = 500, string message = "")
    {
        throw new SimApiException(code, message);
    }

    /// <summary>
    /// 检测条件，根据条件返回报错 如果condition是ture报错
    /// </summary>
    /// <param name="condition">检测条件</param>
    /// <param name="code">错误代码</param>
    /// <param name="message">错误描述</param>
    public static void ErrorWhen([DoesNotReturnIf(true)] bool condition, int code = 400,
        string message = "")
    {
        if (condition)
        {
            Error(code, message);
        }
    }


    /// <summary>
    /// 检测条件，根据条件返回报错 如果condition是ture报错
    /// </summary>
    /// <param name="condition">检测条件</param>
    /// <param name="code">错误代码</param>
    /// <param name="message">错误描述</param>
    public static void ErrorWhenTrue([DoesNotReturnIf(true)] bool condition, int code = 400,
        string message = "")
    {
        ErrorWhen(condition, code, message);
    }


    /// <summary>
    /// 如果condition是false 报错
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="code"></param>
    /// <param name="message"></param>
    public static void ErrorWhenFalse([DoesNotReturnIf(false)] bool condition, int code = 400,
        string message = "")
    {
        ErrorWhen(!condition, code, message);
    }

    /// <summary>
    /// 检测给定的变量是否为NUll
    /// </summary>
    /// <param name="condition">检测条件</param>
    /// <param name="code">错误代码</param>
    /// <param name="message">错误描述</param>
    public static void ErrorWhenNull([NotNull] object? condition, int code = 404,
        string message = "请求的资源不存在")
    {
        ErrorWhen(condition == null, code, message);
    }

    /// <summary>
    /// 检测给定的泛型变量是否为Null（提供更精确的编译器 null 流分析）
    /// </summary>
    /// <param name="condition">检测条件</param>
    /// <param name="code">错误代码</param>
    /// <param name="message">错误描述</param>
    public static void ErrorWhenNull<T>([NotNull] T? condition, int code = 404,
        string message = "请求的资源不存在") where T : class
    {
        ErrorWhen(condition == null, code, message);
    }
}