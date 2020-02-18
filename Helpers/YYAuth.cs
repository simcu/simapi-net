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
        public string Set(int id, string type = "user")
        {
            return Set(id, new[] { type });
        }

        /// <summary>
        /// 产生一个TOken并记录用户ID角色[多角色]
        /// </summary>
        /// <param name="id"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public string Set(int id, string[] type)
        {
            var uuid = Guid.NewGuid().ToString();
            var loginItem = new YYLoginItem
            {
                Id = id,
                Type = type
            };
            Cache.SetString(uuid, JsonSerializer.Serialize(loginItem));
            return uuid;
        }

    }
}
