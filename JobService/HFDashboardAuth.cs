using System;
using Hangfire.Dashboard;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace YYApi.JobService
{
    /// <summary>
    /// Redis缓存Key定义
    /// </summary>
    public struct CacheKey
    {
        public const string HangfireDashboardAuthPrefix = "{hangfire}:dashboard:auth:";
    }

    /// <summary>
    /// HangFire Dashboard Digest认证.
    /// </summary>
    public class HFDashboardAuth : IDashboardAuthorizationFilter
    {
        private IConfiguration _config { get; }
        private IDatabase _redis { get; }
        public HFDashboardAuth(IConfiguration config, IDatabase redis)
        {
            _redis = redis;
            _config = config;
        }

        public bool Authorize(DashboardContext context)
        {
            var http = context.GetHttpContext();
            if (http.Request.Headers.ContainsKey("Authorization"))
            {
                var authObj = _processAuthHeader(http.Request.Headers["Authorization"].ToString());
                if (http.Request.QueryString.ToString().Contains("logout"))
                {
                    _redis.KeyDelete(CacheKey.HangfireDashboardAuthPrefix + authObj["opaque"]);
                    _redirect(http);
                    return true;
                }
                if (authObj["username"] == _config["Hangfire:User"])
                {
                    var a1 = _md5(string.Format("{0}:Need Login:{1}", authObj["username"], _config["Hangfire:Pass"]));
                    var a2 = _md5(string.Format("{0}:{1}", http.Request.Method, authObj["uri"]));
                    var nonce = _redis.StringGet(CacheKey.HangfireDashboardAuthPrefix + authObj["opaque"]);
                    var validCode = _md5(string.Format("{0}:{1}:{2}:{3}:{4}:{5}", a1, nonce, authObj["nc"], authObj["cnonce"], authObj["qop"], a2));
                    if (authObj["response"] == validCode)
                    {
                        _redis.StringSet(CacheKey.HangfireDashboardAuthPrefix + authObj["opaque"], nonce, new TimeSpan(0, 5, 0));
                        return true;
                    }
                }
            }
            _challenge(http);
            return false;
        }

        /// <summary>
        /// 生成401认证header
        /// </summary>
        /// <returns>The challenge.</returns>
        private void _challenge(HttpContext http)
        {
            var response = http.Response;
            var guid = Guid.NewGuid().ToString();
            var opaque = _md5(guid);
            _redis.StringSet(CacheKey.HangfireDashboardAuthPrefix + opaque, guid, new TimeSpan(0, 0, 30));
            response.StatusCode = 401;
            response.Headers.Add("WWW-Authenticate", string.Format("Digest realm=\"Need Login\",qop=\"auth\",nonce=\"{0}\",opaque=\"{1}\"", guid, opaque));
        }

        /// <summary>
        /// 跳转到页面不附加参数
        /// </summary>
        /// <param name="http">Http.</param>
        private void _redirect(HttpContext http)
        {
            var response = http.Response;
            response.StatusCode = 302;
            response.Headers.Add("Location", (http.Request.PathBase + http.Request.Path).ToString());
        }

        /// <summary>
        /// 处理DigestHeader中的参数为字典
        /// </summary>
        /// <returns>The auth header.</returns>
        /// <param name="authData">Auth data.</param>
        private Dictionary<string, string> _processAuthHeader(string authData)
        {
            var authDataArray = authData.Replace("Digest ", string.Empty).Replace("\"", string.Empty).Split(", ");
            var authDic = new Dictionary<string, string>();
            foreach (var item in authDataArray)
            {
                var tmp = item.Split("=", 2);
                authDic.Add(tmp[0], tmp[1]);
            }
            return authDic;
        }

        /// <summary>
        /// 计算字符串32位MD5
        /// </summary>
        /// <returns>The md5.</returns>
        /// <param name="source">Source.</param>
        private string _md5(string source)
        {
            byte[] sor = Encoding.UTF8.GetBytes(source);
            var md5 = MD5.Create();
            byte[] result = md5.ComputeHash(sor);
            StringBuilder strbul = new StringBuilder(40);
            for (int i = 0; i < result.Length; i++)
            {
                strbul.Append(result[i].ToString("x2"));//加密结果"x2"结果为32位,"x3"结果为48位,"x4"结果为64位

            }
            return strbul.ToString();

        }
    }
}
