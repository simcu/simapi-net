namespace SimApi.Communications;

/// <summary>
/// 只有ID的请求
/// </summary>
public class SimApiIdOnlyRequest
{
    public required int Id { get; set; }
}

/// <summary>
/// 只有ID的请求(字符串)
/// </summary>
public class SimApiStringIdOnlyRequest
{
    public required string Id { get; set; }
}

/// <summary>
/// 动态类型单字段请求
/// </summary>
/// <typeparam name="T"></typeparam>
public class SimApiOneFieldRequest<T>
{
    public required T Data { get; set; }
}

/// <summary>
/// 基础分页请求
/// </summary>
public class SimApiBasePageRequest
{
    public required int Page { get; set; }
    public required int Count { get; set; }
}