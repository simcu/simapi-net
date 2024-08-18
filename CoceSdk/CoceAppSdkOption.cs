namespace SimApi.CoceSdk;

public class CoceAppSdkOption
{
    /**
     * 中心API服务器地址
     */
    public string ApiEndpoint { get; set; } = "https://api.coce.cc";

    public string AuthEndpoint { get; set; } = "https://home.coce.cc";

    public string? AppId { get; set; }

    public string? AppKey { get; set; }
}