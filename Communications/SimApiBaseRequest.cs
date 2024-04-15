using System.ComponentModel.DataAnnotations;

namespace SimApi.Communications;

/// <summary>
/// 只有ID的请求
/// </summary>
public class SimApiIdOnlyRequest
{
    [Required] public int Id { get; set; }
}

/// <summary>
/// 只有ID的请求(字符串)
/// </summary>
public class SimApiStringIdOnlyRequest
{
    [Required] public string Id { get; set; }
}

/// <summary>
/// 动态类型单字段请求
/// </summary>
/// <typeparam name="T"></typeparam>
public class SimApiOneFieldRequest<T>
{
    [Required] public T Data { get; set; }
}

/// <summary>
/// 基础分页请求
/// </summary>
public class SimApiBasePageRequest
{
    [Required] public int Page { get; set; }
    [Required] public int Count { get; set; }
}