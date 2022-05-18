using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Unicode;

namespace SimApi.Helpers
{
    public static class SimApiUtil
    {
        /// <summary>
        /// 检测手机号是否正确
        /// </summary>
        /// <param name="cell">手机号码</param>
        /// <returns></returns>
        public static bool CheckCell(string cell)
        {
            var regex = new Regex("^1[3456789]\\d{9}$");
            return regex.IsMatch(cell);
        }

        /// <summary>
        /// MD5加密字符串
        /// </summary>
        /// <param name="source">源字符串</param>
        /// <param name="mode">加密结果"x2"结果为32位,"x3"结果为48位,"x4"结果为64位</param>
        /// <returns></returns>
        public static string Md5(string source, string mode = "x2")
        {
            byte[] sor = Encoding.UTF8.GetBytes(source);
            MD5 md5 = MD5.Create();
            byte[] result = md5.ComputeHash(sor);
            StringBuilder strbul = new StringBuilder(40);
            for (int i = 0; i < result.Length; i++)
            {
                strbul.Append(result[i].ToString(mode));
            }

            return strbul.ToString();
        }

        /// <summary>
        /// 将对象序列化成JSON （控制台输出中文不会被编码）
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string Json(object obj)
        {
            return JsonSerializer.Serialize(obj, new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
            });
        }

        /// <summary>
        /// 获取 CST 当前时间
        /// </summary>
        /// <returns></returns>
        public static DateTime GetCstNow()
        {
            return DateTime.UtcNow.AddHours(8);
        }
    }
}