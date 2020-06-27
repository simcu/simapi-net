using System;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using YYApi.Communications;

namespace YYApi.Helpers
{
    /// <summary>
    /// 认证助手
    /// </summary>
    public class YYAuth
    {
        private IDistributedCache Cache { get; }

        public YYAuth(IDistributedCache cache)
        {
            Cache = cache;
        }

        /// <summary>
        /// 产生一个Token记录并返回Token
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public string Login(int id, string type = "user", string token = null)
        {
            return Login(id, new[] { type }, token);
        }

        /// <summary>
        /// 产生一个Token并记录用户ID角色[多角色]
        /// </summary>
        /// <param name="id"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public string Login(int id, string[] type, string uuid = null)
        {
            if (uuid == null)
            {
                uuid = Guid.NewGuid().ToString();
            }
            var loginItem = new YYLoginItem
            {
                Id = id,
                Type = type
            };
            Cache.SetString(uuid, JsonSerializer.Serialize(loginItem));
            return uuid;
        }

        /// <summary>
        /// 退出登陆
        /// </summary>
        /// <param name="uuid">登陆标识</param>
        public void Logout(string uuid)
        {
            Cache.Remove(uuid);
        }

    }
}
