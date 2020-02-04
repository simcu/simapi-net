using System;
namespace YYApi.Communications
{
    /// <summary>
    /// 只有ID的请求
    /// </summary>
    public class YYIdOnlyRequest
    {
        public int Id { get; set; }
    }

    /// <summary>
    /// 基础分页请求
    /// </summary>
    public class YYBasePageRequest
    {
        public int Page { get; set; }
        public int Count { get; set; }
    }
}
