using System;

namespace SimApi.Communications
{
    /// <summary>
    /// 登录信息中间件
    /// </summary>
    public class SimApiLoginItem
    {
        //登录用户的ID
        public string Id { get; set; }

        //登录用户来源
        public string[] Type { get; set; }
    }
}