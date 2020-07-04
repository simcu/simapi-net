using System;
using Microsoft.AspNetCore.Mvc;
using YYApi.Attributes;
using YYApi.Communications;
using YYApi.Helpers;

namespace YYApi.Controllers
{
    public class YYCommonController : YYBaseController
    {
        private YYAuth Auth { get; }

        public YYCommonController(YYAuth auth)
        {
            Auth = auth;
        }

        /// <summary>
        /// 错误回馈页面
        /// </summary>
        /// <param name="code">错误代码</param>
        /// <returns></returns>
        [HttpGet("exception/{code:int}")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public YYBaseResponse ExceptionHandler(int code)
        {
            var response = new YYBaseResponse();
            response.SetCode(code);
            return response;
        }

        /// <summary>
        /// 检测用户登陆的控制器
        /// </summary>
        /// <returns></returns>
        [HttpPost("/auth/check"), YYDoc("认证", "检测登陆")]
        public YYBaseResponse<int> CheckLogin()
        {
            ErrorWhenNull(LoginInfo, 401);
            return new YYBaseResponse<int> { Data = LoginInfo.Id };
        }

        /// <summary>
        /// 退出登陆
        /// </summary>
        /// <returns></returns>
        [HttpPost("/auth/logout"), YYDoc("认证", "退出登陆")]
        public YYBaseResponse Logout()
        {
            string token = null;

            if (Request.Headers.ContainsKey("Token"))
            {
                token = Request.Headers["Token"];
            }
            Auth.Logout(token);
            return new YYBaseResponse();
        }
    }
}
