# YYApi 基于Asp.net Core 3的一个基础辅助包

![CI](https://github.com/YY-Tech/YYApi/workflows/CI/badge.svg)

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
        protected LoginInfoItem LoginInfo => (LoginInfoItem) HttpContext.Items["LoginInfo"];
    }
}
```

2. 基于Swagger的API文档服务

```C#
#Startup.cs

	services.AddYYDoc("文档名称", "文档描述");

	app.UseYYDoc("名称",SubmitMethod[])

#控制器中可以直接使用特性

	[YYDoc("分组","名称")]
```

3. 简单的基于Redis的登录TOKEN服务

```C#
#Startup.cs
service.AddYYAuth();
```
添加时候, 可以从DI中获取 Auth 类,调用 Auth.Set(int,string) 将用户ID/类型和生成的Token绑定,本方法返回缓存中的Key名称

```C#
#Startup.cs
app.UseYYAuth();
```
调用本中间件,然后再需要登录认证的地方,使用 [YYAuth] 特性,即可完成检测登录相关的操作,
如果需要获取用户的ID, 只需要 直接使用 LoginId 属性即可获取

4. 统一返回
所有Response 均需要继承 BaseResponse类


5. 异常处理
本异常中间件封装了API错误的异常处理,控制器可以使用 Error(int,string) 直接返回API错误

```C#
#Startup.cs
app.UseYYException();
```


### TODO:
1. 增加HANGFIRE 支持redis和sqlite 存储, 支持 基于其他系统的TOKEN认证和独立账号密码认证
