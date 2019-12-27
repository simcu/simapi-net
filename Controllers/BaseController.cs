using YYApi.Communications;
using YYApi.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Linq;

namespace YYApi.Controllers
{
    /// <summary>
    /// 基础控制器,所有控制器均继承本控制器
    /// 1. 自动验证请求参数
    /// 2. 报错返回
    /// 3. 错误回馈页面
    /// </summary>
    public class BaseController : Controller
    {
        /// <summary>
        /// 当前登录用户的ID
        /// </summary>
        protected int LoginId => HttpContext.Items["LoginId"] == null ? 0 : int.Parse(HttpContext.Items["LoginId"].ToString());

        /// <summary>
        /// 验证请求参数
        /// </summary>
        /// <param name="context"></param>
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                foreach (var item in context.ModelState.Values)
                {
                    if (item.ValidationState == ModelValidationState.Invalid)
                    {
                        Error(400, item.Errors.First().ErrorMessage);
                        break;
                    }
                }
            }
        }


        /// <summary>
        /// 错误返回
        /// </summary>
        /// <param name="code">错误代码</param>
        /// <param name="message">错误描述(若是常规错误,代码可自动带取描述)</param>
        /// <returns></returns>
        protected static void Error(int code, string message = null)
        {
            throw new ApiException(code, message);
        }

        /// <summary>
        /// 检测条件，根据条件返回报错
        /// </summary>
        /// <param name="condition">检测条件</param>
        /// <param name="code">错误代码</param>
        /// <param name="message">错误描述</param>
        protected static void ErrorWhen(bool condition, int code, string message = null)
        {
            if (condition)
            {
                Error(code, message);
            }
        }

        /// <summary>
        /// 错误回馈页面
        /// </summary>
        /// <param name="code">错误代码</param>
        /// <returns></returns>
        [HttpGet("exception/{code:int}")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public BaseResponse ExceptionHandler(int code)
        {
            var response = new BaseResponse();
            response.SetCode(code);
            return response;
        }
    }
}