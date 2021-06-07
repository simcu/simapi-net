using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using Minio;
using Minio.Exceptions;
using SimApi.Configs;

namespace SimApi.Helpers
{
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
            var endpoint = string.Empty;
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
            if (useSsl)
            {
                Mc = new MinioClient(endpoint, options.AccessKey, options.SecretKey).WithSSL();
            }
            else
            {
                Mc = new MinioClient(endpoint, options.AccessKey, options.SecretKey);
            }

            bool found = Mc.BucketExistsAsync(Bucket).Result;
            if (!found)
            {
                Mc.MakeBucketAsync(Bucket).Wait();
            }
        }

        public string UploadFile(string path, Stream stream)
        {
            try
            {
                Mc.PutObjectAsync(Bucket, path, stream, stream.Length).Wait();
                return null;
            }
            catch (MinioException e)
            {
                Console.WriteLine("Error occurred: " + e);
                return e.Message;
            }
        }

        public string FullUrl(string path)
        {
            var httpRequest = HttpContextAccessor.HttpContext.Request;
            var url = $"{httpRequest.Scheme}://{httpRequest.Host}";
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            return path.StartsWith("~") ? url + path.Substring(1, path.Length - 1) : $"{ServeUrl}{path}";
        }
    }
}