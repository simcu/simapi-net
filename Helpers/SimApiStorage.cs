#nullable enable
using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using Minio;
using Minio.DataModel.Args;
using SimApi.Configurations;

namespace SimApi.Helpers;

public class SimApiStorage
{
    private IMinioClient Mc { get; }

    public IMinioClient Client => Mc;

    private string ServeUrl { get; }

    private string Endpoint { get; }

    public string Bucket { get; }

    private IHttpContextAccessor HttpContextAccessor { get; }

    public SimApiStorage(SimApiOptions apiOptions, IHttpContextAccessor httpContextAccessor)
    {
        var options = apiOptions.SimApiStorageOptions;
        HttpContextAccessor = httpContextAccessor;
        Endpoint = options.Endpoint;
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
        if (ServeUrl.EndsWith('/')) throw new Exception("SimApiStorage: ServeUrl must not end with /");
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
    public GetUploadUrlResponse GetUploadUrl(string path, int expire = 7200)
    {
        CheckPath(path);
        var obj = path.TrimStart('/');
        var uploadUrl = Mc.PresignedPutObjectAsync(new PresignedPutObjectArgs().WithBucket(Bucket)
            .WithObject(obj).WithExpiry(expire)).Result;
        return new GetUploadUrlResponse(uploadUrl, $"{ServeUrl}{path}", path);
    }

    /// <summary>
    /// 获取下载的URL
    /// </summary>
    /// <param name="path"></param>
    /// <param name="expire"></param>
    /// <returns></returns>
    public string GetDownloadUrl(string path, int expire = 600)
    {
        CheckPath(path);
        path = path.TrimStart('/');
        return Mc.PresignedGetObjectAsync(new PresignedGetObjectArgs().WithBucket(Bucket).WithObject(path)
            .WithExpiry(expire)).Result;
    }


    /// <summary>
    /// 直接上传文件
    /// </summary>
    /// <param name="path"></param>
    /// <param name="stream"></param>
    /// <param name="contentType"></param>
    public void UploadFile(string path, Stream stream, string contentType = "image/png")
    {
        CheckPath(path);
        path = path.TrimStart('/');
        Mc.PutObjectAsync(new PutObjectArgs().WithBucket(Bucket).WithObject(path).WithObjectSize(stream.Length)
            .WithStreamData(stream).WithContentType(contentType)).Wait();
    }

    /// <summary>
    /// 使用path获取完整的访问URL
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public string FullUrl(string path)
    {
        if(path.StartsWith("http://") || path.StartsWith("https://")) return path;
        if (!(path.StartsWith('/') || path.StartsWith("~/"))) throw new Exception("path must start with / or ~/");
        var httpRequest = HttpContextAccessor.HttpContext?.Request;
        var url = $"{httpRequest?.Scheme}://{httpRequest?.Host}";
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }

        return path.StartsWith('~') ? string.Concat(url, path.AsSpan(1, path.Length - 1)) : $"{ServeUrl}{path}";
    }

    /// <summary>
    /// 从URL中获取相对路径 (如果url不是当前服务器的url,则原样返回)
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    public string? GetPath(string? url)
    {
        return url?.Replace($"{Endpoint}/{Bucket}", string.Empty);
    }

    private void CheckPath(string path)
    {
        if (!path.StartsWith('/')) throw new Exception("path must start with /");
    }
}

public record GetUploadUrlResponse(string UploadUrl, string DownloadUrl, string Path);