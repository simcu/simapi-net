using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using SimApi.Helpers;

namespace SimApi.Communications;

/// <summary>
/// 基础相应
/// </summary>
public class SimApiBaseResponse(int code = 200, string message = "成功")
{
    public int Code { get; set; } = code;
    public string Message { get; set; } = message;

    public SimApiBaseResponse() : this(200, "成功")
    {
    }

    /// <summary>
    /// 默认错误代码对应提示信息
    /// </summary>
    private static readonly Dictionary<int, string> MsgBox = new()
    {
        { 200, "成功" },
        { 204, "没有数据" },
        { 400, "参数错误" },
        { 401, "需要登录" },
        { 403, "无权访问" },
        { 404, "接口不存在" },
        { 500, "服务器错误" }
    };

    public SimApiBaseResponse(int code) : this(code, MsgBox.GetValueOrDefault(code, "未知错误"))
    {
    }

    /// <summary>
    /// 序列化为JSON字符串
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return SimApiUtil.Json(this);
    }
}

/// <summary>
/// 分页内容返回
/// </summary>
/// <typeparam name="T"></typeparam>
public class PageResponse<T>
{
    public T? List { get; set; }
    public int Page { get; set; } = 1;
    public int Count { get; set; } = 20;
    public int Total { get; set; }
}

/// <summary>
/// 动态Data返回
/// </summary>
/// <typeparam name="T"></typeparam>
public class SimApiBaseResponse<T>() : SimApiBaseResponse
{
    public T? Data { get; set; }

    public SimApiBaseResponse(T data) : this()
    {
        Data = data;
    }
}