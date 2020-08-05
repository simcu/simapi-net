using System;
using Swashbuckle.AspNetCore.Annotations;

namespace SimApi.Attributes
{
    /// <summary>
    /// 快捷自定义接口文档类
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class SimApiDocAttribute : SwaggerOperationAttribute
    {
        /// <summary>
        /// 定义接口说明
        /// </summary>
        /// <param name="tag">接口分组</param>
        /// <param name="name">接口名称</param>
        public SimApiDocAttribute(string tag, string name)
        {
            Tags = new[] { tag };
            Summary = name;
            // Consumes = new[] {"application/json"};
            // Produces = new[] {"application/json"};
        }
    }
}