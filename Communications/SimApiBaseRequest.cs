namespace SimApi.Communications;

/// <summary>
/// 只有ID的请求
/// </summary>
public record SimApiIdOnlyRequest(int Id);

/// <summary>
/// 只有ID的请求(字符串)
/// </summary>
public record SimApiStringIdOnlyRequest(string Id);

/// <summary>
/// 动态类型单字段请求
/// </summary>
/// <typeparam name="T"></typeparam>
public record SimApiOneFieldRequest<T>(T Data);

/// <summary>
/// 基础分页请求
/// </summary>
public record SimApiBasePageRequest(int Page, int Count);