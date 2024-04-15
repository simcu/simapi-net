using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using Minio;
using SimApi.Configurations;

namespace SimApi.Helpers;

public class SimApiStorage
{
    private MinioClient Mc { get; }

    public MinioClient Client => Mc;

    private string ServeUrl { get; }

    private string Bucket { get; }

    private IHttpContextAccessor HttpContextAccessor { get; }

    public SimApiStorage(SimApiOptions apiOptions, IHttpContextAccessor httpContextAccessor)
    {
        var options = apiOptions.SimApiStorageOptions;
        HttpContextAccessor = httpContextAccessor;
        var useSsl = false;
        string endpoint;
        if (options.Endpoint.StartsWith("http://"))
        {
            endpoint = options.Endpoint.Replace("http://", string.Empty);
        }
        else if (options.Endpoint.StartsWith("https://"))
        {
            endpoint = options.Endpoint.Replace("https://", string.Empty);
            useSsl = true;
        }
        else
        {
            throw new Exception("SimApiStorage: Error Endpoint");
        }

        ServeUrl = options.ServeUrl;
        Bucket = options.Bucket;
        var mcb = new MinioClient().WithEndpoint(endpoint)
            .WithCredentials(options.AccessKey, options.SecretKey);
        if (useSsl)
        {
            mcb = mcb.WithSSL();
        }

        Mc = mcb.Build();

        var found = Mc.BucketExistsAsync(new BucketExistsArgs().WithBucket(Bucket)).Result;
        if (!found)
        {
            Mc.MakeBucketAsync(new MakeBucketArgs().WithBucket(Bucket)).Wait();
        }
    }

    /// <summary>
    /// 获取上传的URL
    /// </summary>
    /// <param name="path"></param>
    /// <param name="expire"></param>
    /// <returns></returns>
    public string GetUploadUrl(string path, int expire = 7200)
    {
        return Mc.PresignedPutObjectAsync(new PresignedPutObjectArgs().WithBucket(Bucket)
            .WithObject(path).WithExpiry(expire)).Result;
    }

    /// <summary>
    /// 获取下载的URL
    /// </summary>
    /// <param name="path"></param>
    /// <param name="expire"></param>
    /// <returns></returns>
    public string GetDownloadUrl(string path, int expire = 600)
    {
        return Mc.PresignedGetObjectAsync(new PresignedGetObjectArgs().WithBucket(Bucket).WithObject(path)
            .WithExpiry(expire)).Result;
    }


    public string UploadFile(string path, Stream stream, string contentType = "image/png")
    {
        Mc.PutObjectAsync(new PutObjectArgs().WithBucket(Bucket).WithObject(path).WithObjectSize(stream.Length)
            .WithStreamData(stream).WithContentType(contentType)).Wait();
        return null;
    }

    public string FullUrl(string path)
    {
        var httpRequest = HttpContextAccessor.HttpContext?.Request;
        var url = $"{httpRequest?.Scheme}://{httpRequest?.Host}";
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }

        return path.StartsWith('~') ? string.Concat(url, path.AsSpan(1, path.Length - 1)) : $"{ServeUrl}{path}";
    }
}