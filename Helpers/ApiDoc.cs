using Swashbuckle.AspNetCore.Annotations;

namespace YYApi.Helpers
{
    /// <summary>
    /// 快捷自定义接口文档类
    /// </summary>
    public class ApiDoc : SwaggerOperationAttribute
    {
        /// <summary>
        /// 定义接口说明
        /// </summary>
        /// <param name="tag">接口分组</param>
        /// <param name="name">接口名称</param>
        public ApiDoc(string tag, string name)
        {
            Tags = new[] {tag};
            Summary = name;
            // Consumes = new[] {"application/json"};
            // Produces = new[] {"application/json"};
        }
    }
}