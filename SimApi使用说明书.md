# SimApi 库使用说明书

## 1. 项目概述

SimApi 是一个基于 .NET 的基础辅助包，提供了一系列实用功能，帮助开发者快速构建和部署 API 服务。

### 主要功能特性：

- **统一的参数检测和错误处理**：自动验证请求参数并返回标准化的错误响应
- **基础认证服务**：基于 Header Token 的简单认证机制
- **S3 兼容的存储系统**：支持文件上传、下载和管理
- **任务调度系统**：基于 Hangfire 的后台任务管理
- **事件和 RPC 调用**：基于 RabbitMQ 的事件和 RPC 通信
- **自定义日志格式**：提供格式化的控制台日志
- **在线 API 文档**：基于 Swagger 的 API 文档生成
- **统一的响应格式**：标准化的 API 响应结构
- **CORS 配置**：支持跨域资源共享
- **版本管理**：提供应用版本和 SimApi 版本查询

## 2. 安装方法

### 通过 NuGet 安装：

```bash
Install-Package SimApi
```

### 项目集成

在 `Startup.cs` 或 `Program.cs` 中配置 SimApi：

```csharp
// 在 ConfigureServices 方法中
services.AddSimApi(options =>
{
    // 配置选项
});

// 在 Configure 方法中
app.UseSimApi();
```

## 3. 核心功能模块

### 3.1 基础控制器

所有控制器应继承自 `SimApiBaseController`，以获得统一的参数检测和错误处理功能。

```csharp
using SimApi.Controllers;

public class BaseController : SimApiBaseController
{
    /// <summary>
    /// 获取登录用户信息
    /// </summary>
    protected SimApiLoginItem LoginInfo => (SimApiLoginItem) HttpContext.Items["LoginInfo"];
}
```

### 3.2 认证服务

#### 配置认证服务：

```csharp
services.AddSimApi(options =>
{
    options.EnableSimApiAuth = true;
});
```

#### 使用认证：

1. 在控制器或动作方法上添加 `[SimApiAuth]` 属性
2. 登录用户信息可通过 `LoginInfo` 属性获取

#### 认证相关接口：

- `POST /auth/check`：检测用户登录状态
- `POST /auth/logout`：用户退出登录
- `POST /user/info`：获取用户信息

### 3.3 存储服务

#### 配置存储服务：

```csharp
services.AddSimApi(options =>
{
    options.EnableSimApiStorage = true;
    options.SimApiStorageOptions = Configuration.GetSection("S3").Get<SimApiStorageOptions>();
});
```

#### 存储配置选项：

```json
{
  "S3": {
    "Endpoint": "http://localhost:9000",
    "AccessKey": "minioadmin",
    "SecretKey": "minioadmin",
    "Bucket": "mybucket",
    "ServeUrl": "http://localhost:9000/mybucket"
  }
}
```

#### 使用存储服务：

```csharp
private readonly SimApiStorage _storage;

public MyController(SimApiStorage storage)
{
    _storage = storage;
}

// 获取上传 URL
var uploadUrlResponse = _storage.GetUploadUrl("/path/to/file.txt");

// 获取下载 URL
var downloadUrl = _storage.GetDownloadUrl("/path/to/file.txt");

// 直接上传文件
using var stream = new MemoryStream();
_storage.UploadFile("/path/to/file.txt", stream, "text/plain");

// 获取完整访问 URL
var fullUrl = _storage.FullUrl("/path/to/file.txt");
```

### 3.4 任务调度系统

#### 配置任务调度：

```csharp
services.AddSimApi(options =>
{
    options.EnableJob = true;
    options.SimApiJobOptions = new SimApiJobOptions
    {
        DashboardUrl = "/jobs",
        DashboardAuthUser = "admin",
        DashboardAuthPass = "Admin@123!",
        RedisConfiguration = "localhost:6379",
        Servers = new[]
        {
            new SimApiJobServerConfig
            {
                Queues = new[] { "default" },
                WorkerNum = 50
            }
        }
    };
});
```

#### 使用任务调度：

```csharp
// 立即执行任务
BackgroundJob.Enqueue(() => Console.WriteLine("Hello, world!"));

// 延迟执行任务
BackgroundJob.Schedule(() => Console.WriteLine("Delayed job"), TimeSpan.FromMinutes(1));

// 重复执行任务
RecurringJob.AddOrUpdate("my-recurring-job", () => Console.WriteLine("Recurring job"), Cron.Hourly);

// 连续执行任务
var id = BackgroundJob.Enqueue(() => Console.WriteLine("First job"));
BackgroundJob.ContinueWith(id, () => Console.WriteLine("Second job"));
```

### 3.5 事件和 RPC 调用

#### 配置事件和 RPC：

```csharp
services.AddSimApi(options =>
{
    options.EnableSynapse = true;
    options.SimApiSynapseOptions = new SimApiSynapseOptions
    {
        // 配置选项
    };
});
```

#### 使用事件：

```csharp
// 发布事件
var synapse = serviceProvider.GetRequiredService<Synapse>();
synapse.PublishEvent("event-name", data);

// 订阅事件
[SynapseEvent("event-name")]
public void HandleEvent(dynamic data)
{
    // 处理事件
}
```

#### 使用 RPC：

```csharp
// 发布 RPC 调用
var result = await synapse.CallRpcAsync<string>("rpc-method", data);

// 实现 RPC 方法
[SynapseRpc("rpc-method")]
public string GetData(dynamic data)
{
    return "Hello, RPC!";
}
```

### 3.6 在线 API 文档

#### 配置 API 文档：

```csharp
services.AddSimApi(options =>
{
    options.EnableSimApiDoc = true;
    options.ConfigureSimApiDoc(docOptions =>
    {
        docOptions.ApiGroups = new[]
        {
            new SimApiDocGroupOption
            {
                Id = "admin",
                Name = "后台管理接口",
                Description = "本接口调用需要Scope：sac.api.admin"
            },
            new SimApiDocGroupOption
            {
                Id = "user-v1",
                Name = "用户中心接口",
                Description = "本接口调用需要Scope：sac.api.user"
            }
        };
        docOptions.ApiAuth = new SimApiAuthOption
        {
            Type = new[] { "ClientCredentials", "Implicit", "AuthorizationCode" },
            Scopes = new Dictionary<string, string>
            {
                { "sac.api.user", "用户信息接口权限" },
                { "sac.api.admin", "后台管理API" }
            },
            AuthorizationUrl = "/connect/authorize",
            TokenUrl = "/connect/token"
        };
    });
});
```

#### 访问 API 文档：

启动应用后，访问 `/swagger` 查看 API 文档。

### 3.7 统一响应格式

#### 配置响应过滤器：

```csharp
services.AddSimApi(options =>
{
    options.EnableSimApiResponseFilter = true;
});
```

#### 响应过滤器实现：

SimApi 提供了 `SimApiResponseFilter` 结果过滤器，用于自动封装 API 响应为统一格式：

- 自动将 `null` 结果封装为 `{"Code": 200, "Message": "成功"}`
- 自动将普通对象结果封装为 `{"Code": 200, "Message": "成功", "Data": 对象}`
- 自动将 `EmptyResult` 封装为 `{"Code": 200, "Message": "成功"}`
- 保持 `SimApiBaseResponse` 类型的结果不变

#### 异常中间件：

SimApi 还提供了 `SimApiExceptionMiddleware` 异常中间件，用于统一处理异常：

- 捕获所有未处理的异常
- 将异常转换为标准化的错误响应格式
- 处理 HTTP 状态码，如 404 等
- 记录错误日志

#### 使用响应格式：

```csharp
// 无数据响应
return new SimApiBaseResponse();

// 带数据响应
return new SimApiBaseResponse<User>(user);

// 直接返回对象，会自动被封装
return user;

// 错误响应
Error(400, "参数错误");

// 条件错误检查
ErrorWhenNull(user, 404, "用户不存在");
ErrorWhen(user.Age < 18, 403, "未满18岁，无权访问");
```

#### 原始响应标记：

如果需要返回原始响应格式，不使用统一封装，可以在控制器或动作方法上添加 `[OriginResponse]` 属性：

```csharp
[HttpGet]
[OriginResponse] // 返回原始响应格式
public string GetRawData()
{
    return "原始字符串响应";
}
```

## 4. API 参考

### 4.1 核心类

#### SimApiUtil

**命名空间**：`SimApi.Helpers`

**描述**：提供一系列静态工具方法和属性，用于常见操作。

**主要属性**：

- `CstNow`：获取当前 CST（中国标准时间）
- `JsonOption`：JSON 序列化常规选项
- `SimApiVersion`：获取 SimApi 库版本
- `AppVersion`：获取应用版本
- `TimestampNow`：获取当前秒级时间戳

**主要方法**：

- `CheckCell(string cell)`：检测手机号是否正确
- `Md5(string source, string mode = "x2")`：MD5 加密字符串
- `Sha1(string source, string mode = "x2")`：SHA1 加密字符串
- `XmlDeserialize<T>(string source)`：将 XML 字符串序列化为对象
- `Json(object? obj)`：将对象序列化为 JSON 字符串
- `Paginate<T>(this IQueryable<T> query, int page, int count)`：分页扩展方法

**使用示例**：

```csharp
// 获取当前时间
var now = SimApiUtil.CstNow;

// JSON 序列化
var json = SimApiUtil.Json(new { Name = "Test", Age = 18 });

// MD5 加密
var md5 = SimApiUtil.Md5("password");

// 分页
var query = dbContext.Users.AsQueryable();
var paginatedQuery = query.Paginate(1, 10);

// 获取版本信息
var simApiVersion = SimApiUtil.SimApiVersion;
var appVersion = SimApiUtil.AppVersion;
```

#### SimApiExtensions

**命名空间**：`SimApi`

**描述**：提供一系列扩展方法，用于配置和使用 SimApi。

**主要方法**：

- `AddSimApi(this IServiceCollection builder, Action<SimApiOptions>? options = null)`：向服务集合添加 SimApi 服务和配置
- `UseSimApi(this IHost builder)`：在主机上使用 SimApi
- `UseSimApi(this WebApplication builder)`：在 Web 应用上使用 SimApi，配置中间件和路由

**使用示例**：

```csharp
// 在 ConfigureServices 方法中
services.AddSimApi(options =>
{
    // 配置选项
    options.EnableSimApiDoc = true;
    options.EnableSimApiAuth = true;
    // 其他配置...
});

// 在 Configure 方法中
app.UseSimApi();
```

#### SimApiBaseController

**继承自**：`Controller`

**主要方法**：

- `Error(int code = 500, string message = "")`：抛出错误异常
- `ErrorWhen(bool condition, int code = 400, string message = "")`：当条件为真时抛出错误
- `ErrorWhenNull(object? condition, int code = 404, string message = "请求的资源不存在")`：当对象为 null 时抛出错误
- `UploadFile()`：上传文件

**属性**：

- `LoginInfo`：获取当前登录用户信息

#### SimApiAuth

**主要方法**：

- `Login(SimApiLoginItem loginItem, string? token = null)`：登录用户并返回 token
- `Update(SimApiLoginItem loginItem, string token)`：更新用户登录信息
- `GetLogin(string token)`：根据 token 获取登录信息
- `Logout(string uuid)`：退出登录

#### SimApiStorage

**主要方法**：

- `GetUploadUrl(string path, int expire = 7200)`：获取文件上传 URL
- `GetDownloadUrl(string path, int expire = 600)`：获取文件下载 URL
- `UploadFile(string path, Stream stream, string contentType = "image/png")`：上传文件
- `FullUrl(string? path)`：获取完整的文件访问 URL
- `GetUrl(string? path)`：获取文件访问 URL
- `GetPath(string? url)`：从 URL 中获取相对路径

#### SimApiBaseResponse

**构造函数**：

- `SimApiBaseResponse(int code = 200, string message = "成功")`：创建响应对象

**属性**：

- `Code`：响应代码
- `Message`：响应消息

#### SimApiBaseResponse<T>

**继承自**：`SimApiBaseResponse`

**构造函数**：

- `SimApiBaseResponse(T data)`：创建带数据的响应对象

**属性**：

- `Data`：响应数据

### 4.2 配置类

#### SimApiOptions

**主要属性**：

- `RedisConfiguration`：Redis 配置字符串
- `EnableJob`：是否启用任务调度系统
- `EnableSimApiAuth`：是否启用认证服务
- `EnableCoceSdk`：是否启用 CoceSdk
- `EnableSimApiStorage`：是否启用存储服务
- `EnableSimApiDoc`：是否启用 API 文档
- `EnableSynapse`：是否启用事件和 RPC
- `EnableCors`：是否启用 CORS
- `EnableSimApiException`：是否启用异常拦截
- `EnableSimApiResponseFilter`：是否启用响应过滤器
- `EnableForwardHeaders`：是否启用 Header 转发
- `EnableLowerUrl`：是否启用小写 URL
- `EnableVersionUrl`：是否启用版本查询
- `EnableLogger`：是否启用自定义日志

**配置方法**：

- `ConfigureSimApiDoc(Action<SimApiDocOptions>? options = null)`：配置 API 文档
- `ConfigureSimApiStorage(Action<SimApiStorageOptions>? options = null)`：配置存储服务
- `ConfigureSimApiJob(Action<SimApiJobOptions>? options = null)`：配置任务调度
- `ConfigureSimApiSynapse(Action<SimApiSynapseOptions>? options = null)`：配置事件和 RPC
- `ConfigureCoceSdk(Action<CoceAppSdkOption>? options = null)`：配置 CoceSdk

## 5. 配置选项

### 5.1 存储配置 (SimApiStorageOptions)

```csharp
public class SimApiStorageOptions
{
    public string? Endpoint { get; set; } // S3 服务端点
    public string? AccessKey { get; set; } // 访问密钥
    public string? SecretKey { get; set; } // 密钥
    public string? Bucket { get; set; } // 存储桶名称
    public string? ServeUrl { get; set; } // 访问 URL
}
```

### 5.2 任务调度配置 (SimApiJobOptions)

```csharp
public class SimApiJobOptions
{
    public string? DashboardUrl { get; set; } = "/jobs"; // Web UI 地址
    public string DashboardAuthUser { get; set; } = "admin"; // Web UI 用户名
    public string DashboardAuthPass { get; set; } = "Admin@123!"; // Web UI 密码
    public string? RedisConfiguration { get; set; } // Redis 配置
    public int? Database { get; set; } = null; // Redis 数据库
    public SimApiJobServerConfig[] Servers { get; set; } = [new()]; // 服务器配置
}

public class SimApiJobServerConfig
{
    public string[] Queues { get; set; } = ["default"]; // 队列名称
    public int WorkerNum { get; set; } = 50; // 工作线程数
}
```

### 5.3 API 文档配置 (SimApiDocOptions)

```csharp
public class SimApiDocOptions
{
    public string DocumentTitle { get; set; } = "API 文档"; // 文档标题
    public SimApiDocGroupOption[] ApiGroups { get; set; } = []; // API 分组
    public SimApiAuthOption ApiAuth { get; set; } = new(); // 认证配置
    public string[] SupportedMethod { get; set; } = ["GET", "POST", "PUT", "DELETE"]; // 支持的 HTTP 方法
}

public class SimApiDocGroupOption
{
    public string Id { get; set; } = "api"; // 分组 ID
    public string Name { get; set; } = "API"; // 分组名称
    public string Description { get; set; } = ""; // 分组描述
}

public class SimApiAuthOption
{
    public string[] Type { get; set; } = []; // 认证类型
    public Dictionary<string, string> Scopes { get; set; } = []; // 权限范围
    public string AuthorizationUrl { get; set; } = "/connect/authorize"; // 授权 URL
    public string TokenUrl { get; set; } = "/connect/token"; // Token URL
    public string Description { get; set; } = ""; // 认证描述
}
```

## 6. 使用示例

### 6.1 完整配置示例

```csharp
services.AddSimApi(options =>
{
    // 配置 Redis
    options.RedisConfiguration = "localhost:6379";
    
    // 配置 API 文档
    options.EnableSimApiDoc = true;
    options.ConfigureSimApiDoc(docOptions =>
    {
        docOptions.ApiGroups = new[]
        {
            new SimApiDocGroupOption
            {
                Id = "admin",
                Name = "后台管理接口",
                Description = "本接口调用需要Scope：sac.api.admin"
            },
            new SimApiDocGroupOption
            {
                Id = "user-v1",
                Name = "用户中心接口",
                Description = "本接口调用需要Scope：sac.api.user"
            }
        };
        docOptions.ApiAuth = new SimApiAuthOption
        {
            Type = new[] { "ClientCredentials", "Implicit", "AuthorizationCode" },
            Scopes = new Dictionary<string, string>
            {
                { "sac.api.user", "用户信息接口权限" },
                { "sac.api.admin", "后台管理API" }
            },
            AuthorizationUrl = "/connect/authorize",
            TokenUrl = "/connect/token"
        };
    });
    
    // 配置存储服务
    options.EnableSimApiStorage = true;
    options.SimApiStorageOptions = Configuration.GetSection("S3").Get<SimApiStorageOptions>();
    
    // 配置任务调度
    options.EnableJob = true;
    options.ConfigureSimApiJob(jobOptions =>
    {
        jobOptions.DashboardUrl = "/jobs";
        jobOptions.DashboardAuthUser = "admin";
        jobOptions.DashboardAuthPass = "Admin@123!";
    });
    
    // 配置事件和 RPC
    options.EnableSynapse = true;
    
    // 其他配置
    options.EnableCors = true;
    options.EnableSimApiException = true;
    options.EnableSimApiResponseFilter = true;
    options.EnableVersionUrl = true;
    options.EnableLogger = true;
});

// 使用 SimApi
app.UseSimApi();
```

### 6.2 控制器示例

```csharp
using Microsoft.AspNetCore.Mvc;
using SimApi.Controllers;
using SimApi.Helpers;

[ApiController]
[Route("[controller]")]
public class UserController : BaseController
{
    private readonly SimApiStorage _storage;
    
    public UserController(SimApiStorage storage)
    {
        _storage = storage;
    }
    
    [HttpGet("{id}")]
    public SimApiBaseResponse<User> GetUser(int id)
    {
        var user = GetUserFromDatabase(id);
        ErrorWhenNull(user, 404, "用户不存在");
        return new SimApiBaseResponse<User>(user);
    }
    
    [HttpPost]
    [SimApiAuth] // 需要认证
    public SimApiBaseResponse<User> CreateUser(UserCreateDto dto)
    {
        ErrorWhen(string.IsNullOrEmpty(dto.Name), 400, "用户名不能为空");
        ErrorWhen(dto.Age < 18, 400, "年龄必须大于18岁");
        
        var user = CreateUserInDatabase(dto);
        return new SimApiBaseResponse<User>(user);
    }
    
    [HttpPost("upload-avatar")]
    [SimApiAuth]
    public async Task<SimApiBaseResponse<string>> UploadAvatar(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        var path = $"/avatars/{LoginInfo.Id}/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        _storage.UploadFile(path, stream, file.ContentType);
        var url = _storage.GetUrl(path);
        return new SimApiBaseResponse<string>(url);
    }
}
```

### 6.3 任务调度示例

```csharp
public class UserService
{
    public void SendWelcomeEmail(string email)
    {
        // 发送欢迎邮件
        Console.WriteLine($"Sending welcome email to {email}");
    }
    
    public void CleanupInactiveUsers()
    {
        // 清理不活跃用户
        Console.WriteLine("Cleaning up inactive users");
    }
    
    public void GenerateMonthlyReport()
    {
        // 生成月度报告
        Console.WriteLine("Generating monthly report");
    }
}

// 配置任务
public void ConfigureJobs(IServiceProvider serviceProvider)
{
    // 立即发送欢迎邮件
    BackgroundJob.Enqueue<UserService>(x => x.SendWelcomeEmail("user@example.com"));
    
    // 每天凌晨清理不活跃用户
    RecurringJob.AddOrUpdate<UserService>("cleanup-inactive-users", x => x.CleanupInactiveUsers(), Cron.Daily);
    
    // 每月1日生成月度报告
    RecurringJob.AddOrUpdate<UserService>("generate-monthly-report", x => x.GenerateMonthlyReport(), "0 0 1 * *");
}
```

## 7. 最佳实践

### 7.1 控制器设计

- 所有控制器应继承自 `SimApiBaseController` 或其派生类
- 使用 `Error` 和 `ErrorWhen` 系列方法进行错误处理
- 对需要认证的接口使用 `[SimApiAuth]` 属性
- 合理使用 API 分组，便于文档管理

### 7.2 存储管理

- 为不同类型的文件使用不同的存储路径结构
- 合理设置文件 URL 的过期时间
- 对上传的文件进行验证和处理
- 考虑使用 CDN 加速文件访问

### 7.3 任务调度

- 合理设置任务的队列和优先级
- 对长时间运行的任务进行分解
- 监控任务的执行状态和结果
- 合理设置任务的重试策略

### 7.4 事件和 RPC

- 为事件和 RPC 方法使用清晰的命名规范
- 合理设计事件和 RPC 的数据结构
- 考虑事件处理的幂等性
- 监控事件和 RPC 的执行情况

### 7.5 配置管理

- 使用配置文件或环境变量管理配置
- 对敏感配置进行加密处理
- 不同环境使用不同的配置
- 定期审查和更新配置

### 7.6 性能优化

- 合理使用缓存减少数据库访问
- 对高频访问的接口进行优化
- 考虑使用异步方法提高并发性能
- 监控系统性能并进行调优

## 8. 故障排查

### 8.1 常见问题

#### 认证失败
- 检查 Token 是否正确
- 检查 Redis 是否正常运行
- 检查认证中间件是否正确配置

#### 存储服务错误
- 检查 S3 服务是否正常运行
- 检查存储配置是否正确
- 检查网络连接是否正常

#### 任务调度错误
- 检查 Hangfire 仪表盘是否可访问
- 检查 Redis 是否正常运行
- 检查任务代码是否有异常

#### API 文档生成错误
- 检查 Swagger 配置是否正确
- 检查控制器和方法的注释是否完整
- 检查模型类是否有循环引用

### 8.2 日志和监控

- 启用 `EnableLogger` 配置查看详细日志
- 使用应用性能监控工具监控系统状态
- 定期检查系统日志和错误报告
- 设置关键指标的告警机制

## 9. 版本管理

- 访问 `/versions` 查看应用版本和 SimApi 版本
- 定期更新 SimApi 到最新版本
- 注意版本升级时的兼容性问题
- 遵循语义化版本规范管理应用版本

## 10. 总结

SimApi 是一个功能丰富的 .NET 基础辅助包，提供了一系列实用功能，帮助开发者快速构建和部署 API 服务。通过合理配置和使用 SimApi，可以显著提高开发效率，减少重复代码，提高系统的可维护性和可靠性。

本说明书提供了 SimApi 的详细使用方法和最佳实践，希望能帮助开发者更好地使用这个库。如果有任何问题或建议，欢迎反馈和贡献。