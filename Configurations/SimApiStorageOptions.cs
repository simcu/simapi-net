namespace SimApi.Configs;

public class SimApiStorageOptions
{
    /// <summary>
    /// S3 服务器入口地址
    /// </summary>
    public string Endpoint { get; set; }

    /// <summary>
    /// S3 服务器文件访问地址
    /// </summary>
    public string ServeUrl { get; set; }

    /// <summary>
    /// S3服务 Bucket
    /// </summary>
    public string Bucket { get; set; }

    public string AccessKey { get; set; }

    public string SecretKey { get; set; }
}