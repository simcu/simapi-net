# SimApi 使用说明书

> **NuGet 包名**：`Simcu.SimApi`  
> **目标框架**：`net8.0` / `net9.0` / `net10.0`  
> **作者**：xRain@SimcuTeam

---

## 1. 项目概述

SimApi 是一个基于 ASP.NET Core 的 **API 开发基础辅助库**，核心理念是**一行注册、一行启用**。通过 `AddSimApi()` + `UseSimApi()` 即可获得统一响应格式、认证、文档、对象存储、任务调度、MQTT 消息通信等全套能力。

### 主要功能特性

| 功能 | 说明 |
|------|------|
| **统一响应格式** | 自动封装所有接口响应为 `{code, message, data}` 格式 |
| **全局异常处理** | 捕获所有异常并以 JSON 格式返回，HTTP 状态码始终为 200 |
| **Token 认证** | 基于 Redis + Header Token 的简单认证机制 |
| **在线 API 文档** | 基于 Swagger 的多分组文档，含认证标注、签名参数自动注入 |
| **S3 对象存储** | MinIO 兼容的文件存储，支持预签名上传/下载 |
| **任务调度** | 基于 Hangfire + Redis 的后台任务管理，带 Web 控制台 |
| **MQTT 消息通信** | 基于 MQTTnet v5 的事件发布/订阅与 RPC 调用（Synapse 模块） |
| **API 签名验证** | MD5 签名 + 时间戳过期 + 防重放攻击 |
| **AES 加密传输** | 自动解密 AES-256-CBC 加密的请求体 |
| **Redis 缓存** | 统一封装的分布式缓存操作 |
| **Coce 统一身份** | 集成 Coce 第三方身份认证平台 |
| **彩色控制台日志** | 按级别着色的格式化控制台日志 |
| **CORS** | 开发/生产环境全量跨域支持 |
| **反向代理支持** | ForwardedHeaders 透传，获取真实 IP |

---

## 2. 安装

```bash
dotnet add package Simcu.SimApi
```

---

## 3. 快速开始

`Program.cs` 中两步完成集成：

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSimApi(options =>
{
    options.RedisConfiguration = "localhost:6379";
    options.EnableSimApiAuth = true;
    options.EnableSimApiDoc = true;

    options.ConfigureSimApiDoc(doc =>
    {
        doc.DocumentTitle = "我的API文档";
        doc.ApiGroups =
        [
            new("api",   "公共接口"),
            new("admin", "管理接口", "需要管理员Token")
        ];
    });
});

var app = builder.Build();
app.UseSimApi();  // 一行启用全部中间件和路由
app.Run();
```

---

## 4. 配置选项（SimApiOptions）

所有配置通过 `AddSimApi(options => { ... })` 设置。

### 4.1 功能开关

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `RedisConfiguration` | `string?` | `null` | Redis 连接字符串（多个模块依赖） |
| `EnableSimApiAuth` | `bool` | `false` | 启用 Token 认证体系 |
| `EnableSimApiDoc` | `bool` | `false` | 启用 Swagger 文档（`/swagger`） |
| `EnableSimApiStorage` | `bool` | `false` | 启用 S3 对象存储 |
| `EnableJob` | `bool` | `false` | 启用 Hangfire 任务调度 |
| `EnableSynapse` | `bool` | `false` | 启用 MQTT 消息/RPC 通信 |
| `EnableCoceSdk` | `bool` | `false` | 启用 Coce 统一身份平台 |
| `EnableCors` | `bool` | **`true`** | 启用 CORS 全部允许 |
| `EnableSimApiException` | `bool` | **`true`** | 启用全局异常拦截 |
| `EnableSimApiResponseFilter` | `bool` | **`true`** | 启用响应统一封装 |
| `EnableForwardHeaders` | `bool` | **`true`** | 启用反向代理 Header 转发 |
| `EnableLowerUrl` | `bool` | **`true`** | URL 强制小写 |
| `EnableVersionUrl` | `bool` | **`true`** | 启用 `/versions` 接口 |
| `EnableLogger` | `bool` | **`true`** | 启用彩色控制台日志 |

### 4.2 子模块配置方法

```csharp
options.ConfigureSimApiDoc(doc => { ... });
options.ConfigureSimApiStorage(s => { ... });
options.ConfigureSimApiJob(job => { ... });
options.ConfigureSimApiSynapse(s => { ... });
options.ConfigureCoceSdk(coce => { ... });
```

---

## 5. 核心模块

### 5.1 统一响应格式

#### 5.1.1 响应结构

所有接口统一返回以下 JSON 格式：

```json
{
  "code": 200,
  "message": "成功",
  "data": { ... }
}
```

#### 5.1.2 状态码映射

| code | 默认 message |
|------|-------------|
| 200  | 成功 |
| 204  | 没有数据 |
| 400  | 参数错误 |
| 401  | 需要登录 |
| 403  | 无权访问 |
| 404  | 接口不存在 |
| 500  | 服务器错误 |

#### 5.1.3 响应类

```csharp
// 无数据响应
return new SimApiBaseResponse();
return new SimApiBaseResponse(400, "参数错误");
return new SimApiBaseResponse(404);  // 自动映射消息：接口不存在

// 带数据响应
return new SimApiBaseResponse<User>(user);

// 分页响应
return new SimApiBaseResponse<PageResponse<List<User>>>(new PageResponse<List<User>>
{
    List = users,
    Page = 1,
    Count = 20,
    Total = 100
});
```

#### 5.1.4 响应过滤器行为（SimApiResponseFilter）

| 控制器返回值 | 最终 JSON 输出 |
|------------|--------------|
| `null` | `{"code":200,"message":"成功"}` |
| 普通对象 | `{"code":200,"message":"成功","data":对象}` |
| `SimApiBaseResponse` | 原样输出 |
| 标注 `[OriginResponse]` | 完全不封装，原始输出 |

#### 5.1.5 跳过封装

```csharp
[HttpGet]
[OriginResponse]
public string GetRaw() => "raw string";
```

---

### 5.2 基础控制器（SimApiBaseController）

所有业务控制器建议继承 `SimApiBaseController`：

```csharp
[ApiController]
[Route("[controller]")]
public class UserController : SimApiBaseController
{
    // 获取当前登录用户（需配合 EnableSimApiAuth）
    // protected SimApiLoginItem LoginInfo => ...
}
```

#### 错误处理方法（均为 `protected static`）

| 方法 | 触发条件 | 默认参数 |
|------|----------|---------|
| `Error(code, message)` | 直接抛出 | `500, ""` |
| `ErrorWhen(condition, code, message)` | condition == `true` | `400, ""` |
| `ErrorWhenTrue(condition, code, message)` | 同上（别名） | `400, ""` |
| `ErrorWhenFalse(condition, code, message)` | condition == `false` | `400, ""` |
| `ErrorWhenNull(obj, code, message)` | obj == `null` | `404, "请求的资源不存在"` |

```csharp
public SimApiBaseResponse<User> GetUser(int id)
{
    var user = _db.Users.Find(id);
    ErrorWhenNull(user, 404, "用户不存在");

    ErrorWhen(user.Age < 18, 403, "未满18岁");

    return new SimApiBaseResponse<User>(user);
}
```

---

### 5.3 认证模块（SimApiAuth）

#### 5.3.1 配置

```csharp
options.EnableSimApiAuth = true;  // 需同时配置 RedisConfiguration
```

#### 5.3.2 工作原理

1. 客户端请求时在 Header 中携带 `Token: <token_value>`
2. `SimApiAuthMiddleware` 从 Redis 读取登录信息存入 `HttpContext.Items["LoginInfo"]`
3. `[SimApiAuth]` Attribute 检查是否已登录及角色权限

#### 5.3.3 SimApiLoginItem 结构

```csharp
public class SimApiLoginItem
{
    public required string Id { get; set; }               // 用户唯一标识
    public string[] Type { get; set; } = ["user"];        // 用户类型（支持多角色）
    public Dictionary<string, string> Meta { get; set; } = []; // 附加元数据
    public object? Extra { get; set; }                    // 扩展数据
}
```

#### 5.3.4 SimApiAuth 助手方法

```csharp
// 注入使用
public MyController(SimApiAuth auth) { ... }

// 登录（token 不传则自动生成 GUID）
string token = auth.Login(loginItem);
string token = auth.Login(loginItem, "custom-token");

// 更新登录信息（不改变 token）
auth.Update(loginItem, token);

// 获取登录信息
SimApiLoginItem? info = auth.GetLogin(token);

// 退出登录
auth.Logout(token);
```

#### 5.3.5 自动注册路由

启用 `EnableSimApiAuth` 后自动生成以下路由：

| 路由 | 方法 | 说明 |
|------|------|------|
| `POST /auth/check` | 无需登录 | 检测登录状态，返回用户 ID |
| `POST /auth/logout` | 无需登录 | 退出登录 |
| `POST /user/info` | 需要登录 | 获取当前用户完整信息 |

---

### 5.4 特性（Attributes）

#### `[SimApiAuth]` — 身份认证

```csharp
[SimApiAuth]                     // 仅检查是否登录
[SimApiAuth("admin")]            // 检查 Type 中是否含 "admin"
[SimApiAuth("admin,manager")]    // 检查 Type 是否含 "admin" 或 "manager"（逗号分隔）
[SimApiAuth(new[]{"a", "b"})]    // 数组形式
```

#### `[SimApiDoc]` — Swagger 文档注解

```csharp
[SimApiDoc("用户管理", "获取用户列表")]
[SimApiDoc("用户管理", "创建用户", "创建一个新用户账号，需要管理员权限")]
[SimApiDoc(new[]{"用户", "管理"}, "批量操作")]
```

#### `[SimApiSign]` — API 签名验证

需配合实现 `SimApiSignProviderBase` 并注入：

```csharp
[SimApiSign(KeyProvider = typeof(MySignProvider))]
public IActionResult SecureApi(...) { ... }
```

签名算法：`MD5(field1=v1&...&appId=xxx&timestamp=ts&nonce=nnn&密钥)`

#### `[AesBody]` — AES 加密请求体

客户端发送：`{"data": "AES密文"}`，服务端自动解密并反序列化：

```csharp
[HttpPost]
public IActionResult Process([AesBody] MyRequest request)
{
    // request 已自动解密并反序列化
}
```

#### `[OriginResponse]` — 跳过响应封装

```csharp
[HttpGet]
[OriginResponse]
public string GetRawData() => "raw data";
```

#### `[SynapseEvent]` — MQTT 事件处理方法

```csharp
// 参数1：事件名；参数2（可选）：事件数据
[SynapseEvent("order/created")]
public void OnOrderCreated(string eventName) { ... }

[SynapseEvent("order/+/status")]  // 支持 MQTT 通配符 + 和 #
public void OnOrderStatus(string eventName, MyEventData data) { ... }
```

#### `[SynapseRpc]` — MQTT RPC 方法

```csharp
[SynapseRpc]                    // 方法名自动为 "ClassName.MethodName"
[SynapseRpc("getUserInfo")]     // 自定义 RPC 方法名

// 0~2 个参数，第2个参数固定为 Dictionary<string,string>（headers）
public UserDto GetUserInfo(GetUserRequest req) { ... }
public UserDto GetUserInfo(GetUserRequest req, Dictionary<string, string> headers) { ... }
```

---

### 5.5 在线 API 文档（Swagger）

#### 5.5.1 配置

```csharp
options.EnableSimApiDoc = true;
options.ConfigureSimApiDoc(doc =>
{
    doc.DocumentTitle = "接口文档";

    // 接口分组（Id 对应 ApiExplorerSettings.GroupName）
    doc.ApiGroups =
    [
        new("api",   "公共接口"),
        new("admin", "管理接口", "需要管理员Token")
    ];

    // 配置认证方式（支持 SimApiAuth / ClientCredentials / Implicit / AuthorizationCode / Password）
    doc.ApiAuth = new SimApiAuthOption
    {
        Type = ["SimApiAuth"]  // 默认
    };

    // 支持的调试方法（默认仅 POST）
    doc.SupportedMethod = [SubmitMethod.Post, SubmitMethod.Get];
});
```

#### 5.5.2 接口分组

```csharp
// 归入 admin 文档
[ApiExplorerSettings(GroupName = "admin")]
public class AdminController : SimApiBaseController { ... }

// 不标注则默认归入 Id="api" 的文档
```

#### 5.5.3 访问文档

启动后访问 `/swagger`。

#### 5.5.4 自动 Swagger 增强

SimApi 内置多个 Swagger 过滤器，无需手动配置：

| 过滤器 | 效果 |
|--------|------|
| `SimApiResponseOperationFilter` | 自动将返回类型包装为 `SimApiBaseResponse<T>` 显示 |
| `SimApiAuthOperationFilter` | 为 `[SimApiAuth]` 接口添加 Token 认证要求 |
| `SimApiSignOperationFilter` | 为 `[SimApiSign]` 接口自动注入签名参数说明 |
| `AesBodyOperationFilter` | 为 `[AesBody]` 参数展示加密前的原始数据结构 |
| `GlobalDynamicObjectSchemaFilter` | 为 `object`/`Dictionary` 类型生成合理 Schema 示例 |
| `RemoveEmptyTagsFilter` | 清除没有接口的空分组 Tag |

---

### 5.6 S3 对象存储（SimApiStorage）

基于 MinIO SDK（S3 兼容协议）。

#### 5.6.1 配置

```csharp
options.EnableSimApiStorage = true;
options.ConfigureSimApiStorage(s =>
{
    s.Endpoint  = "http://minio.example.com:9000";  // 注意不能以 / 结尾
    s.AccessKey = "admin";
    s.SecretKey = "password";
    s.Bucket    = "my-bucket";
    s.ServeUrl  = "http://cdn.example.com/my-bucket";  // 注意不能以 / 结尾
});
```

#### 5.6.2 使用

```csharp
public MyController(SimApiStorage storage) { ... }

// 获取预签名上传 URL（默认 2 小时过期），路径必须以 / 开头
GetUploadUrlResponse r = storage.GetUploadUrl("/avatars/user1.jpg");
// r.UploadUrl   → 上传用的预签名 URL（供前端直接 PUT 请求）
// r.DownloadUrl → 下载用的公开 URL
// r.Path        → 相对路径

// 获取预签名下载 URL（默认 10 分钟过期）
string url = storage.GetDownloadUrl("/files/doc.pdf");
string url = storage.GetDownloadUrl("/files/doc.pdf", expire: 3600);

// 直接服务端上传
storage.UploadFile("/avatars/user1.jpg", stream, "image/jpeg");

// 路径转公开访问 URL
// - 以 http/https 开头：原样返回
// - 以 ~/ 开头：转当前请求域名的本地路径
// - 以 / 开头：拼接 ServeUrl
string? url = storage.FullUrl("/path/to/file");
string? url = storage.GetUrl("/path/to/file");

// URL 转相对路径
string? path = storage.GetPath("http://cdn.example.com/my-bucket/path/file");

// 暴露底层 MinIO 客户端
IMinioClient mc = storage.Client;
```

---

### 5.7 任务调度（Hangfire）

基于 Hangfire + Redis 存储。

#### 5.7.1 配置

```csharp
options.EnableJob = true;
options.ConfigureSimApiJob(job =>
{
    job.DashboardUrl      = "/jobs";        // null 则不启用 Dashboard
    job.DashboardAuthUser = "admin";
    job.DashboardAuthPass = "Admin@123!";
    job.RedisConfiguration = null;          // null 则使用全局 RedisConfiguration
    job.Database = 1;                       // Redis DB 编号，null 则使用默认
    job.Servers =
    [
        new SimApiJobServerConfig { Queues = ["default"], WorkerNum = 5 },
        new SimApiJobServerConfig { Queues = ["email"],   WorkerNum = 2 }
    ];
});
```

#### 5.7.2 使用

```csharp
// 立即执行
BackgroundJob.Enqueue(() => Console.WriteLine("立即执行"));

// 延迟执行
BackgroundJob.Schedule(() => Console.WriteLine("延迟执行"), TimeSpan.FromMinutes(5));

// 定时循环执行
RecurringJob.AddOrUpdate("daily-report",
    () => myService.GenerateReport(), Cron.Daily);

// 依赖执行（上一个完成后）
var jobId = BackgroundJob.Enqueue(() => Console.WriteLine("第一步"));
BackgroundJob.ContinueJobWith(jobId, () => Console.WriteLine("第二步"));
```

Dashboard 访问 `/jobs`，使用 HTTP Basic Auth（配置的用户名密码）。

---

### 5.8 MQTT 消息通信（Synapse）

基于 MQTTnet v5，通过 WebSocket 连接 MQTT Broker。

#### 5.8.1 配置

```csharp
options.EnableSynapse = true;
options.ConfigureSimApiSynapse(s =>
{
    s.Websocket  = "ws://mqtt.example.com:8083/mqtt";
    s.Username   = "user";
    s.Password   = "pass";
    s.SysName    = "my-system";      // 系统名（Topic 命名空间前缀）
    s.AppName    = "order-service";  // 服务名
    s.AppId      = "instance-001";  // 实例 ID（不填则自动生成 GUID）
    s.RpcTimeout = 3;                // RPC 超时秒数，默认 3
    s.EventLoadBalancing  = false;   // 启用事件负载均衡（$queue 订阅），默认 false
    s.EnableConfigStore   = true;    // 启用分布式配置中心，默认 true
    s.DisableEventClient  = false;   // 禁用事件客户端，默认 false
    s.DisableRpcClient    = false;   // 禁用 RPC 客户端，默认 false
});
```

#### 5.8.2 MQTT Topic 规则

| 用途 | Topic 格式 |
|------|-----------|
| 事件发布 | `{SysName}/event/{AppName}/{eventName}` |
| 事件订阅（负载均衡关闭） | `{SysName}/event/{eventName}` |
| 事件订阅（负载均衡开启） | `$queue/{SysName}/event/{eventName}` |
| RPC 请求 | `{SysName}/{targetApp}/rpc/server/{method}`（`$queue` 订阅，天然负载均衡） |
| RPC 响应 | `{SysName}/{callerApp}/rpc/client/{AppId}/{messageId}` |
| 配置存储 | `{SysName}/synapse-config-store/{key}`（Retain 消息） |

#### 5.8.3 Synapse API（通过 DI 注入使用）

```csharp
public MyService(Synapse synapse) { ... }

// 发送事件
synapse.Event("order/created", new { OrderId = 1 });

// 调用 RPC（同步阻塞，返回 SimApiBaseResponse<T>）
var res = synapse.Rpc<UserDto>("user-service", "GetUserInfo", new { Id = 1 });
var res = synapse.Rpc<UserDto>("user-service", "GetUserInfo", param,
    headers: new Dictionary<string, string> { { "traceId", "xxx" } });

// 无类型 RPC
var res = synapse.Rpc("user-service", "GetUserInfo", param);

// 读写分布式配置
synapse.SetConfig("max_retry", "3");
string? value = synapse.GetConfig("max_retry");

// 监听配置变化
synapse.OnConfigChanged += (sender, item) =>
    Console.WriteLine($"{item.Key} = {item.Value}");

// 在 RPC 方法内部抛出错误
synapse.RpcError(400, "参数错误");
synapse.RpcErrorWhen(id <= 0, 400, "ID 无效");
```

#### 5.8.4 注册事件/RPC 处理器

启用 `EnableSynapse` 后，SimApi 会**自动扫描调用程序集**中所有含 `[SynapseRpc]` 或 `[SynapseEvent]` 方法的类，并自动注册为 Scoped 服务。

```csharp
// 事件处理类（无需手动注入，自动注册）
public class OrderEventHandler
{
    [SynapseEvent("order/+/status")]
    public void OnOrderStatus(string eventName, OrderStatusDto data)
    {
        // eventName = "order/123/status"
        // data 已自动反序列化
    }
}

// RPC 服务类（无需手动注入，自动注册）
public class UserRpcService
{
    [SynapseRpc]  // 方法名为 "UserRpcService.GetUserInfo"
    public UserDto GetUserInfo(GetUserRequest req)
    {
        return new UserDto { ... };
    }

    [SynapseRpc("customRpcName")]
    public ResultDto DoSomething(RequestDto req, Dictionary<string, string> headers)
    {
        // headers 包含 RPC 调用方传入的自定义头
    }
}
```

---

### 5.9 API 签名验证（SimApiSign）

#### 5.9.1 实现密钥提供者

```csharp
public class MySignProvider : SimApiSignProviderBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    public MySignProvider(IServiceScopeFactory sf) => _scopeFactory = sf;

    public override string? AppIdName  { get; set; } = "appId";
    public override string TimestampName { get; set; } = "timestamp";
    public override string NonceName   { get; set; } = "nonce";
    public override string SignName    { get; set; } = "sign";
    public override int    QueryExpires { get; set; } = 5;            // 5秒过期
    public override bool   DuplicateRequestProtection { get; set; } = true; // 防重放
    public override string[] SignFields { get; set; } = ["userId"];   // 额外签名字段

    public override string? GetKey(string? appId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        return db.Apps.Find(appId)?.SecretKey;
    }
}

// 注册（Scoped 或 Transient）
services.AddScoped<MySignProvider>();
```

#### 5.9.2 使用

```csharp
[SimApiSign(KeyProvider = typeof(MySignProvider))]
public IActionResult SecureApi(...) { ... }
```

#### 5.9.3 签名算法

```
MD5(field1=v1&field2=v2&...&appId=xxx&timestamp=ts&nonce=nnn&密钥)
```

支持通过 Query 或 Header 传入签名参数。

---

### 5.10 AES 加密传输（AesBody）

算法：**AES-256-CBC + PKCS7 填充**，IV 随机生成并附在密文前（Base64 编码）。

#### 5.10.1 实现密钥提供者

```csharp
public class MyAesKeyProvider : AesBodyProviderBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    public MyAesKeyProvider(IServiceScopeFactory sf) => _scopeFactory = sf;

    public override string? AppIdName { get; set; } = "appId";  // 从 Query/Header 获取

    public override string? GetKey(string? appId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        return db.Apps.Find(appId)?.SecretKey;
    }
}

// 注册
services.AddScoped<MyAesKeyProvider>();
```

#### 5.10.2 使用

```csharp
[HttpPost]
public IActionResult Submit([AesBody(KeyProvider = typeof(MyAesKeyProvider))] MyRequest request)
{
    // request 已自动解密并反序列化
}
```

客户端请求格式（提交 JSON body）：
```json
{"data": "Base64(AES加密后的JSON字符串)"}
```

#### 5.10.3 AES 工具类

```csharp
string cipher = SimApiAesUtil.Encrypt("明文内容", "任意长度密钥");
string plain  = SimApiAesUtil.Decrypt(cipher, "任意长度密钥");
```

密钥会经过 SHA256 处理为 32 字节，因此支持任意长度密钥。

---

### 5.11 Redis 缓存（SimApiCache）

统一加前缀 `SimApi:Cache:`，避免 key 冲突。

```csharp
public MyService(SimApiCache cache) { ... }

// 存储（永不过期）
cache.Set("userCount", 100);

// 存储（带过期时间）
cache.Set("userCount", 100, new DistributedCacheEntryOptions
{
    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
});

// 获取原始字符串
string? raw = cache.Get("userCount");

// 获取并反序列化为指定类型
int? count = cache.Get<int>("userCount");
```

> 注意：`SimApiCache` 要求配置 `RedisConfiguration`。

---

### 5.12 HTTP 客户端（SimApiHttpClient）

用于调用其他带签名/AES 加密的 SimApi 服务。

```csharp
var client = new SimApiHttpClient(appId: "myapp", appKey: "secret")
{
    Server        = "https://api.example.com",
    AppIdName     = "appId",
    TimestampName = "timestamp",
    NonceName     = "nonce",
    SignName      = "sign",
    SignFields    = ["field1", "field2"]
};

// 仅签名请求
var result = client.SignQuery<UserDto>("/api/user", body, queries);

// 仅 AES 加密请求（body 自动加密为 {"data":"Base64密文"}）
var result = client.AesQuery<UserDto>("/api/user", body);

// AES 加密 + 签名
var result = client.AesSignQuery<UserDto>("/api/user", body, queries);
```

---

### 5.13 工具类（SimApiUtil）

全部为静态成员，直接调用，无需注入。

```csharp
// 时间
DateTime cst       = SimApiUtil.CstNow;       // UTC+8 当前时间
double   timestamp = SimApiUtil.TimestampNow;  // 秒级 Unix 时间戳

// 版本
string simApiVer = SimApiUtil.SimApiVersion;  // SimApi 包版本
string appVer    = SimApiUtil.AppVersion;     // 宿主应用版本

// 哈希加密
string md5  = SimApiUtil.Md5("source");           // 32位（mode="x2"）
string md5  = SimApiUtil.Md5("source", "x3");     // 48位
string sha1 = SimApiUtil.Sha1("source");          // SHA1

// 序列化
string json = SimApiUtil.Json(obj);               // camelCase，中文不转义
T obj       = SimApiUtil.XmlDeserialize<T>(xml);  // XML → 对象
JsonSerializerOptions opts = SimApiUtil.JsonOption; // JSON 配置

// 验证
bool ok = SimApiUtil.CheckCell("13800138000");    // 手机号格式验证

// 分页（IQueryable 扩展方法）
var paged = dbContext.Users.AsQueryable().Paginate(page: 1, count: 20);
```

---

### 5.14 数据模型基类（SimApiBaseModel）

ORM 实体基类，提供通用字段和轻量级属性映射能力。

```csharp
public class UserEntity : SimApiBaseModel
{
    public string Name { get; set; }
    // 自动拥有：Id (GUID, string)、CreatedAt、UpdatedAt
}

// 从 DTO 映射到实体（自动跳过 Id / CreatedAt / UpdatedAt）
entity.MapData(dto);

// 强制映射所有字段（包括 Id 等受保护字段）
entity.MapData(dto, mapAll: true);

// 只映射指定字段
entity.MapData(dto, new[] { "Name", "Email" });

// 手动更新 UpdatedAt
entity.UpdateTime();
```

> **注意**：`MapData` 只映射**同名且同类型**且**源值不为 null** 的属性。

---

### 5.15 Coce 统一身份平台（CoceSdk）

集成 Coce 第三方 OAuth 统一身份认证平台（默认 `api.coce.cc`）。

#### 5.15.1 配置

```csharp
options.EnableCoceSdk = true;   // 需同时启用 EnableSimApiAuth
options.ConfigureCoceSdk(coce =>
{
    coce.ApiEndpoint  = "https://api.coce.cc";   // 默认值
    coce.AuthEndpoint = "https://home.coce.cc";  // 默认值
    coce.AppId  = "your-app-id";
    coce.AppKey = "your-app-key";
});
```

#### 5.15.2 自动注册路由

| 路由 | 方法 | 说明 |
|------|------|------|
| `POST /auth/login` | 无需登录 | Coce 一键登录（前端传 `{"data":"lv1Token"}`） |
| `POST /user/groups` | 需要登录 | 获取当前用户群组列表 |
| `GET /auth/config` | 无需登录 | 获取 AppId 和授权 URL |

#### 5.15.3 CoceApp 可用方法

```csharp
public MyService(CoceApp coce) { ... }

// 用户
coce.GetUserInfo(levelToken)               // 获取用户基本信息
coce.GetUserGroups(levelToken)             // 获取用户群组
coce.SearchUserByPhone("13800138000")      // 按手机号搜索用户
coce.SearchUserByIds(new[]{"uid1","uid2"}) // 按 ID 批量获取用户

// 消息
coce.SendUserMessage(userId, "标题", "内容")

// 支付
string? tradeNo = coce.TradeCreate("商品名", 100, "扩展数据") // 创建订单
coce.TradeCheck(tradeNo)                   // 查询订单状态
coce.TradeRefund(tradeNo)                  // 发起退款

// Token 管理
coce.GetLevelToken(lv1Token, level: 5)     // 换取 Level Token
coce.SaveToken(userId, levelToken)         // 保存到 Redis
coce.GetToken(userId)                      // 从 Redis 读取

// 代理请求
coce.ProxyQuery<T>(uri, token)
coce.ProxyQuery<T>(uri, token, json)
coce.ProxyQueue<T>(uri, token, data)
```

#### 5.15.4 自定义登录逻辑

实现 `ICoceLoginProcessor` 接口，在登录时根据群组赋予角色：

```csharp
public class MyLoginProcessor : ICoceLoginProcessor
{
    public SimApiLoginItem Process(SimApiLoginItem item, GroupInfo[] groups)
    {
        if (groups.Any(g => g.Role == "owner"))
            item.Type = ["user", "admin"];
        return item;
    }
}

// 注册
services.AddScoped<ICoceLoginProcessor, MyLoginProcessor>();
```

---

### 5.16 日志（SimApiLogger）

启用后替换默认控制台日志，输出格式如下：

```
[ Microsoft.AspNetCore.Hosting.Diagnostics ][ 2024-01-01 12:00:00:0000 ][ Information ]
Request starting HTTP/1.1 POST http://localhost/api/user
```

| 日志级别 | 控制台颜色 |
|----------|-----------|
| Debug | 深紫色 |
| Information | 深青色 |
| Warning | 黄色 |
| Error | 红色 |
| Critical | 深红色 |

---

## 6. 内置路由汇总

| 路由 | 方法 | 启用条件 | 说明 |
|------|------|----------|------|
| `/swagger` | GET | `EnableSimApiDoc` | API 文档页面 |
| `/versions` | GET/POST | `EnableVersionUrl` | 查看版本信息 |
| `/auth/check` | POST | `EnableSimApiAuth` | 检测登录状态 |
| `/auth/logout` | POST | `EnableSimApiAuth` | 退出登录 |
| `/user/info` | POST | `EnableSimApiAuth` | 获取用户信息（需登录） |
| `/auth/login` | POST | `EnableCoceSdk` | Coce 登录 |
| `/user/groups` | POST | `EnableCoceSdk` | 获取用户群组（需登录） |
| `/auth/config` | POST | `EnableCoceSdk` | 获取 Coce 配置 |
| `/jobs` | GET | `EnableJob` | Hangfire Dashboard |

---

## 7. 异常处理机制

```
HTTP 请求
    ↓
SimApiExceptionMiddleware（最外层，透传 Query-Id Header）
    ├── 捕获 SimApiException → 返回 {code: xxx, message: xxx}
    ├── 捕获 Exception       → 记录日志 + 返回 {code: 500, message: 错误信息}
    └── 响应 404 等非 200/301/302 状态码 → 转换为 SimApiException
         ↓
SimApiAuthMiddleware（解析 Token → HttpContext.Items["LoginInfo"]）
         ↓
[SimApiSign] Filter（签名验证）
         ↓
[SimApiAuth] Filter（登录/角色检查）
         ↓
Controller.OnActionExecuting（模型验证 → 400）
         ↓
Action 方法执行
         ↓
SimApiResponseFilter（自动封装响应）
```

> **关键特性**：所有错误均以 **HTTP 200** 状态码返回，错误信息体现在响应 JSON 的 `code` 字段中。

---

## 8. 完整配置示例

```csharp
builder.Services.AddSimApi(options =>
{
    // Redis（多功能共享）
    options.RedisConfiguration = "localhost:6379";

    // 认证
    options.EnableSimApiAuth = true;

    // Swagger 文档
    options.EnableSimApiDoc = true;
    options.ConfigureSimApiDoc(doc =>
    {
        doc.DocumentTitle = "我的服务接口文档";
        doc.ApiGroups =
        [
            new("api",   "公共接口"),
            new("admin", "管理接口", "需要管理员 Token")
        ];
        doc.ApiAuth = new SimApiAuthOption { Type = ["SimApiAuth"] };
        doc.SupportedMethod = [SubmitMethod.Post];
    });

    // S3 存储
    options.EnableSimApiStorage = true;
    options.ConfigureSimApiStorage(s =>
    {
        s.Endpoint  = "http://minio:9000";
        s.AccessKey = "admin";
        s.SecretKey = "password";
        s.Bucket    = "my-bucket";
        s.ServeUrl  = "http://cdn.example.com/my-bucket";
    });

    // 任务调度
    options.EnableJob = true;
    options.ConfigureSimApiJob(job =>
    {
        job.DashboardUrl      = "/jobs";
        job.DashboardAuthUser = "admin";
        job.DashboardAuthPass = "Admin@123!";
        job.Servers = [new() { Queues = ["default"], WorkerNum = 5 }];
    });

    // MQTT 通信
    options.EnableSynapse = true;
    options.ConfigureSimApiSynapse(s =>
    {
        s.Websocket = "ws://mqtt:8083/mqtt";
        s.Username  = "user";
        s.Password  = "pass";
        s.SysName   = "my-system";
        s.AppName   = "api-service";
    });

    // Coce 统一身份
    options.EnableCoceSdk = true;
    options.ConfigureCoceSdk(coce =>
    {
        coce.AppId  = "your-app-id";
        coce.AppKey = "your-app-key";
    });
});

var app = builder.Build();
app.UseSimApi();
app.Run();
```

---

## 9. 项目依赖

| 包 | 版本 | 用途 |
|----|------|------|
| `Hangfire.AspNetCore` | 1.8.22 | 任务调度框架 |
| `Hangfire.Console` | 1.4.3 | 任务控制台日志 |
| `Hangfire.Redis.StackExchange` | 1.12.0 | Hangfire Redis 存储 |
| `Microsoft.Extensions.Caching.StackExchangeRedis` | 10.0.1 | Redis 分布式缓存 |
| `Minio` | 7.0.0 | S3 对象存储 |
| `MQTTnet` | 5.0.1.1416 | MQTT 消息通信 |
| `Swashbuckle.AspNetCore.Annotations` | 10.1.0 | Swagger 注解 |
| `Swashbuckle.AspNetCore.SwaggerUI` | 10.1.0 | Swagger UI |

---

## 10. 最佳实践

### 10.1 控制器设计

- 所有控制器继承 `SimApiBaseController`
- 优先使用 `ErrorWhenNull` / `ErrorWhen` 系列方法进行前置校验，保持主逻辑清晰
- 需要认证的接口标注 `[SimApiAuth]`，按角色访问控制的传入角色参数
- 用 `[SimApiDoc]` 为每个接口添加文档注解，分组管理

### 10.2 存储管理

- 路径统一以 `/` 开头，按业务分类规划路径结构（如 `/avatars/{userId}/`）
- 对公开资源使用 `GetUrl`，对私密资源使用 `GetDownloadUrl`（设合适过期时间）
- 上传前在业务层验证文件类型和大小

### 10.3 任务调度

- 将长耗时操作异步化，接口立即返回，后台任务处理
- 根据任务类型设置不同队列，避免低优先级任务阻塞高优先级任务
- 定期检查 Hangfire Dashboard，监控失败任务

### 10.4 Synapse 消息通信

- 事件名使用层级路径风格（如 `order/created`、`payment/success`）
- RPC 方法名使用简洁的语义名称
- 为幂等性操作设计事件处理器（同一事件可能被多次投递）
- 超时处理：`Rpc` 返回 `code=502` 表示超时

### 10.5 安全

- Token 认证默认不设过期时间，建议在业务层配合定期清理或设置 Redis TTL
- 签名验证默认开启防重放（5 秒 nonce 缓存），生产环境保持开启
- AES 密钥通过数据库存储，不硬编码在代码中
