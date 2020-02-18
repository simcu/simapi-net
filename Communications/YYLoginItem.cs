using System;
namespace YYApi.Communications
{
    /// <summary>
    /// 登录信息中间件
    /// </summary>
    public class YYLoginItem
    {
        //登录用户的ID
        public int Id { get; set; }
        //登录用户来源
        public string[] Type { get; set; }
    }
}
