using System;

namespace SimApi.Communications
{
    /// <summary>
    /// 只有ID的请求
    /// </summary>
    public class SimApiIdOnlyRequest
    {
        public int Id { get; set; }
    }

    /// <summary>
    /// 动态类型单字段请求
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SimApiOneFieldRequest<T>
    {
        public T Data { get; set; }
    }

    /// <summary>
    /// 基础分页请求
    /// </summary>
    public class SimApiBasePageRequest
    {
        public int Page { get; set; }
        public int Count { get; set; }
    }
}