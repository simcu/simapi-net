using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SimApi.Helpers;

public static class SimApiAesUtil
{
    // 密钥长度：256位 (32字节)
    private const int KeySize = 256;

    // 块大小：128位 (16字节)
    private const int BlockSize = 128;

    // 加密模式 - 重命名以避免与枚举类型冲突
    private const CipherMode AesCipherMode = CipherMode.CBC;

    // 填充模式 - 重命名以避免与枚举类型冲突
    private const PaddingMode AesPaddingMode = PaddingMode.PKCS7;

    /// <summary>
    /// AES 加密
    /// </summary>
    /// <param name="plainText">明文</param>
    /// <param name="key">字符串密钥（将被处理为256位）</param>
    /// <returns>加密后的Base64字符串（包含IV）</returns>
    public static string Encrypt(string plainText, string key)
    {
        if (string.IsNullOrEmpty(plainText))
            throw new ArgumentNullException(nameof(plainText));
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        // 处理密钥为指定长度
        var keyBytes = ProcessKey(key);
        // 生成随机IV
        var iv = GenerateRandomIv();

        using var aes = CreateAesProvider(keyBytes, iv);
        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        // 先写入IV，解密时需要用到
        ms.Write(iv, 0, iv.Length);

        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    /// <summary>
    /// AES 解密
    /// </summary>
    /// <param name="cipherText">加密后的Base64字符串</param>
    /// <param name="key">字符串密钥（与加密时相同）</param>
    /// <returns>解密后的明文</returns>
    public static string Decrypt(string cipherText, string key)
    {
        if (string.IsNullOrEmpty(cipherText))
            throw new ArgumentNullException(nameof(cipherText));
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        var cipherBytes = Convert.FromBase64String(cipherText);

        // 从加密数据中提取IV
        var iv = new byte[BlockSize / 8];
        Array.Copy(cipherBytes, 0, iv, 0, iv.Length);

        // 处理密钥为指定长度
        var keyBytes = ProcessKey(key);

        using var aes = CreateAesProvider(keyBytes, iv);
        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(cipherBytes, iv.Length, cipherBytes.Length - iv.Length);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        return sr.ReadToEnd();
    }

    /// <summary>
    /// 处理密钥为指定长度（256位）
    /// </summary>
    private static byte[] ProcessKey(string key)
    {
        // 使用SHA256哈希处理密钥，确保得到32字节(256位)的密钥
        return SHA256.HashData(Encoding.UTF8.GetBytes(key));
    }

    /// <summary>
    /// 生成随机初始化向量
    /// </summary>
    private static byte[] GenerateRandomIv()
    {
        using var aes = Aes.Create();
        aes.BlockSize = BlockSize;
        aes.GenerateIV();
        return aes.IV;
    }

    /// <summary>
    /// 创建并配置AES加密服务提供器
    /// </summary>
    private static Aes CreateAesProvider(byte[] key, byte[] iv)
    {
        var aes = Aes.Create();
        aes.KeySize = KeySize;
        aes.BlockSize = BlockSize;
        aes.Mode = AesCipherMode;
        aes.Padding = AesPaddingMode;
        aes.Key = key;
        aes.IV = iv;
        return aes;
    }
}