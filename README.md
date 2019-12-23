# YYApi 基于Asp.net Core 3的一个基础辅助包

### 包含了以下组件:

1. 统一的参数检测,基础认证服务
所有的控制器需要继承 YYApi.Controllers.BaseController

```C#
using Models;

namespace Controllers
{
    public class BaseController : YYApi.Controllers.BaseController
    {
        /// <summary>
        /// 获取登录用户信息
        /// </summary>
        protected User LoginInfo => (User) HttpContext.Items["LoginInfo"];
    }
}
```

2. 基于Swagger的API文档服务

```C#
#Startup.cs

	services.AddApiDoc("文档名称", "文档描述");

	app.UseApiDoc("名称",SubmitMethod[])

#控制器中可以直接使用特性

	[ApiDoc("分组","名称")]
```

3. 简单的基于Redis的登录TOKEN服务

```C#
#Startup.cs
service.AddAuth();
```
添加时候, 可以从DI中获取 Auth 类,调用 Auth.SetId(int) 将用户ID和生成的Token绑定,本方法返回缓存中的Key名称

```C#
#Startup.cs
app.UseAuthMiddleware();
```
调用本中间件,然后再需要登录认证的地方,使用 [CheckAuth] 特性,即可完成检测登录相关的操作,
如果需要获取用户的ID, 只需要 直接使用 LoginId 属性即可获取

4. 统一返回
所有Response 均需要继承 BaseResponse类


5. 异常处理
本异常中间件封装了API错误的异常处理,控制器可以使用 Error(int,string) 直接返回API错误

```C#
#Startup.cs
app.UseExceptionMiddleware();
```