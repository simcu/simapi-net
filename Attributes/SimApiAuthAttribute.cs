using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Filters;
using SimApi.Communications;
using SimApi.Exceptions;

namespace SimApi.Attributes
{
    /// <summary>
    /// 检测登录中间件
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class SimApiAuthAttribute : ActionFilterAttribute
    {
        private string[] Types { get; }

        //默认是user登录类型
        public SimApiAuthAttribute()
        {
            Types = new[] { "user" };
        }

        //只检测一种用户类型的快捷方式
        public SimApiAuthAttribute(string type)
        {
            Types = new[] { type };
        }

        //设定特定类型的检测
        public SimApiAuthAttribute(string[] types)
        {
            Types = types;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var loginInfo = (SimApiLoginItem)context.HttpContext.Items["LoginInfo"];
            //检测是否登录
            if (loginInfo == null)
            {
                throw new SimApiException(401);
            }
            //检测用户类型
            if (Types.Intersect(loginInfo.Type).Count() == 0)
            {
                throw new SimApiException(403);
            }
        }
    }
}
