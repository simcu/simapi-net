# SimApi 基于.Net的一个基础辅助包

[![PublishNugetPackage](https://github.com/simcu/simapi-net/actions/workflows/nuget-publish.yml/badge.svg)](https://github.com/simcu/simapi-net/actions/workflows/nuget-publish.yml)
### 包含了以下组件:

1. 统一的参数检测,基础认证服务
所有的控制器需要继承 YYApi.Controllers.BaseController

```C#
using Models;
using SimApi.Controllers;

namespace Controllers
{
    public class BaseController : SimApiBaseController
    {
        /// <summary>
        /// 获取登录用户信息
        /// </summary>
        protected SimApiLoginInfoItem LoginInfo => (SimApiLoginInfoItem) HttpContext.Items["LoginInfo"];
    }
}
```


统一配置，现在只需要进行AddSimApi的配置， Configure中直接 UseSimApi即可。
```C#
services.AddSimApi(options =>
{
   options.ConfigureSimApiDoc(options =>
   {
     options.ApiGroups = new[]
     {
        new SimApiDocGroupOption
            {Id = "admin", Name = "后台管理接口", Description = "本接口调用需要Scope：sac.api.admin"},
        new SimApiDocGroupOption
            {Id = "user-v1", Name = "用户中心接口", Description = "本接口调用需要Scope：sac.api.user"}
     };
     options.ApiAuth = new SimApiAuthOption
       {
         Type = new[] {"ClientCredentials", "Implicit", "AuthorizationCode"},
         Scopes = new Dictionary<string, string>
            {
              {"sac.api.user", "用户信息接口权限"},
              {"sac.api.admin", "后台管理API"}
            },
         AuthorizationUrl = "/connect/authorize",
         TokenUrl = "/connect/token"
       };
     });
     options.EnableSimApiStorage = true;
     options.SimApiStorageOptions = Configuration.GetSection("S3").Get<SimApiStorageOptions>();
});
```
基于S3的存储,基于RabbitMq的事件RPC调用,自定义Logger格式,自定义响应格式
