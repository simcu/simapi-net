using System;
using Microsoft.AspNetCore.Mvc;
using SimApi.Attributes;
using SimApi.Communications;
using SimApi.Helpers;

namespace SimApi.Controllers
{
    [ApiExplorerSettings(GroupName = "api")]
    public class YYCommonController : SimApiBaseController
    {
        private SimApiAuth Auth { get; }

        public YYCommonController(SimApiAuth auth)
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
        public SimApiBaseResponse ExceptionHandler(int code)
        {
            var response = new SimApiBaseResponse();
            response.SetCode(code);
            return response;
        }

        /// <summary>
        /// 检测用户登陆的控制器
        /// </summary>
        /// <returns></returns>
        [HttpPost("/auth/check"), SimApiDoc("认证", "检测登陆")]
        public SimApiBaseResponse<int> CheckLogin()
        {
            ErrorWhenNull(LoginInfo, 401);
            return new SimApiBaseResponse<int> {Data = LoginInfo.Id};
        }

        /// <summary>
        /// 退出登陆
        /// </summary>
        /// <returns></returns>
        [HttpPost("/auth/logout"), SimApiDoc("认证", "退出登陆")]
        public SimApiBaseResponse Logout()
        {
            string token = null;

            if (Request.Headers.ContainsKey("Token"))
            {
                token = Request.Headers["Token"];
            }

            Auth.Logout(token);
            return new SimApiBaseResponse();
        }
    }
}