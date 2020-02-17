using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Filters;
using YYApi.Communications;
using YYApi.Exceptions;

namespace YYApi.Attributes
{
    /// <summary>
    /// 检测登录中间件
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class YYAuthAttribute : ActionFilterAttribute
    {
        private string[] Types { get; }

        //默认是user登录类型
        public YYAuthAttribute()
        {
            Types = new[] { "user" };
        }

        //只检测一种用户类型的快捷方式
        public YYAuthAttribute(string type)
        {
            Types = new[] { type };
        }

        //设定特定类型的检测
        public YYAuthAttribute(string[] types)
        {
            Types = types;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var loginInfo = (YYLoginItem)context.HttpContext.Items["LoginInfo"];
            //检测是否登录
            if (loginInfo == null)
            {
                throw new YYApiException(401);
            }
            //检测用户类型
            if (!Types.Contains(loginInfo.Type))
            {
                throw new YYApiException(403);
            }
        }
    }
}
