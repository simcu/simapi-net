using SimApi.Communications;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Linq;
using static SimApi.Helpers.SimApiError;

namespace SimApi.Controllers;

/// <summary>
/// 基础控制器,所有控制器均继承本控制器
/// 1. 自动验证请求参数
/// 2. 报错返回
/// 3. 错误回馈页面
/// </summary>
///
[Consumes("application/json")]
[Produces("application/json")]
public class SimApiBaseController : Controller
{
    /// <summary>
    /// 当前登录用户的ID
    /// </summary>
    protected SimApiLoginItem LoginInfo => (SimApiLoginItem)HttpContext.Items["LoginInfo"]!;

    protected string LoginToken => (string)HttpContext.Items["LoginToken"]!;

    /// <summary>
    /// 验证请求参数
    /// </summary>
    /// <param name="context"></param>
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ModelState.IsValid) return;
        foreach (var item in context.ModelState.Values.Where(item =>
                     item.ValidationState == ModelValidationState.Invalid))
        {
            Error(400, item.Errors.First().ErrorMessage);
            break;
        }
    }
}