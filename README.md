# SimApi 完整 AI 编码参考

> **NuGet**: `Simcu.SimApi` | **目标框架**: `net8.0` / `net9.0` / `net10.0`
> **作者**: xRain@SimcuTeam | **性质**: ASP.NET Core API 基础辅助库
>
> **使用方式**: 将本文档作为上下文提供给 AI，或粘贴到对话开头。

---

## 0. 快速开始

```csharp
// Program.cs — 两步启动
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
app.UseSimApi();
app.Run();
```

---

## 1. 核心概念（必读）

### 1.1 统一响应格式

**所有接口统一输出 JSON，HTTP 状态码始终 `200`，错误信息在 `code` 字段：**

```json
{ "code": 200, "message": "成功", "data": { ... } }
```

| code 含义 |
|-----------|
| 200 成功 | 204 无数据 | 400 参数错误 |
| 401 需要登录 | 403 无权访问 | 404 不存在 | 500 服务器错误 |

### 1.2 异常处理流程

```
请求进入
 └─ SimApiExceptionMiddleware（捕获所有异常 → HTTP 200 + code）
     └─ SimApiAuthMiddleware（解析 Token → HttpContext.Items["LoginInfo"]）
         └─ [SimApiSign] Filter（签名验证）
             └─ [SimApiAuth] Filter（登录/角色检查）
                 └─ OnActionExecuting（模型验证 → code 400）
                     └─ Action 执行
                         └─ SimApiResponseFilter（封装响应）
```

### 1.3 GOTCHAS（AI 最容易犯的错）

| ❌ 错误 | ✅ 正确 |
|---------|---------|
| `SupportedMethod` 写多种方法 | 默认仅 **POST**，按需显式添加 |
| `WorkerNum = 50` | 默认是 **5** |
| 存储路径 `/avatars/file.jpg`（无前导斜杠） | 路径必须以 **`/`** 开头 |
| `s.Endpoint = "http://minio:9000/"` | `ServeUrl`/`Endpoint` **不能以 `/` 结尾** |
| `synapse.PublishEvent(...)` | 方法名是 **`synapse.Event(...)`** |
| `synapse.CallRpcAsync(...)` | 方法名是 **`synapse.Rpc<T>(...)`** |
| HTTP 状态码 4xx/5xx 表示错误 | 所有错误均 **HTTP 200**，错误在 JSON `code` |
| `SimApiStorageOptions = Configuration.GetSection(...)` | 用 **`options.ConfigureSimApiStorage(s => {...})`** |
| MQTT 用 RabbitMQ | **MQTTnet v5，通过 WebSocket 连接** |

---

## 2. C# 语言与编码规范

### 2.1 技术栈基础

```
NuGet: Simcu.SimApi
.NET 8 / 9 / 10    C# 12 / 14
Nullable: enable    ImplicitUsings: enable
```

### 2.2 命名空间

使用**文件范围命名空间**：

```csharp
// ✅
namespace MyApp.Controllers;

// ❌
namespace MyApp.Controllers { }
```

### 2.3 主构造函数（依赖注入）

```csharp
// ✅ 主构造函数
public class OrderController(DataContext db) : SimApiBaseController { }

// ❌ 传统构造函数
public class OrderController : SimApiBaseController {
    private readonly DataContext _db;
    public OrderController(DataContext db) { _db = db; }
}
```

### 2.4 集合表达式

优先 `[]`：
```csharp
string[] tags = [];                          // ✅
string[] roles = ["admin", "manager"];       // ✅
// var tags = new string[] { };               // ❌
```

### 2.5 Null 处理

```csharp
var key = app?.Key;                         // 安全访问
var name = user?.Name ?? "匿名";            // 空合并
config ??= new Dictionary<string, string>(); // 空合并赋值
```

### 2.6 命名规范

| 类型 | 规则 | 示例 |
|------|------|------|
| 类、接口、枚举 | PascalCase | `AdminController`、`ResPermission` |
| 方法名 | PascalCase | `UserList`、`ApplicationEdit` |
| 属性名 | PascalCase | `AccountId`、`LicenseTotal` |
| 私有字段 | `_camelCase`（如有） | `_logger` |
| 局部变量、参数 | camelCase | `var user`、`var appId` |
| 常量 | PascalCase | `MaxRetryCount`、`DefaultRole` |
| 路由路径 | 全小写 + 连字符 | `/device/refresh-context` |
| 配置键 | PascalCase:PascalCase | `"Sms:Templates:Verify"` |
| Redis 缓存 Key | `模块:子类型:标识` | `"Sms:Verify:登陆:手机号"` |

---

## 3. 项目目录结构

推荐**极简扁平化**：

```
项目名/
├── Controllers/          # 控制器（含业务逻辑）
│   └── Dtos/             # 请求/响应 DTO
├── Models/               # EF Core 实体 + DataContext
├── Helpers/              # 工具类 / 框架扩展点
├── Migrations/           # EF Core 迁移（自动生成，勿手改）
└── Program.cs            # 入口 + DI + 中间件（无 Startup.cs）
```

业务逻辑直接在 Controller 中通过 `db`（EF Core DbContext）操作数据库。可复用横切逻辑抽取到 `Helpers/`。

> 如果项目较复杂也可选标准分层（Controllers → Services → Models），但需项目内保持一致不混用。

---

## 4. 错误处理系统（SimApiError）

### 4.1 概述

报错方法已从 BaseController 移出，独立为静态类 `SimApi.Helpers.SimApiError`。可在任何地方使用。

**使用方式**：每个需要用的文件加一行 `using static SimApiHelpers.SimApiError;` 即可直接调用：

```csharp
using static SimApi.Helpers.SimApiError;
```

> 业务项目可在根目录建 `GlobalUsings.cs` 加 `global using static SimApi.Helpers.SimApiError;` 实现全项目免 import。

### 4.2 方法签名

```csharp
// 直接抛出错误
Error(int code = 500, string message = "");

// condition 为 true 时抛出（默认 code=400）
ErrorWhen([DoesNotReturnIf(true)] bool condition, int code = 400, string message = "");

// ErrorWhen 的别名
ErrorWhenTrue(bool condition, int code = 400, string message = "");

// condition 为 false 时抛出
ErrorWhenFalse([DoesNotReturnIf(false)] bool condition, int code = 400, string message = "");

// obj 为 null 时抛出（默认 code=404）
ErrorWhenNull([NotNull] object? condition, int code = 404,
    string message = "请求的资源不存在");

// 泛型版本（编译器 null 流分析更精准）
ErrorWhenNull<T>([NotNull] T? condition, int code = 404,
    string message = "请求的资源不存在") where T : class;
```

所有方法最终都 `throw new SimApiException(code, message)`，由全局中间件捕获转为 `{code, message}` 响应。

### 4.3 使用示例

```csharp
using static SimApi.Helpers.SimApiError;

// Controller 内
[HttpPost]
public UserDto GetUser(string id)
{
    var user = db.Users.Find(id);
    ErrorWhenNull(user, 404, "用户不存在");          // null → 404
    ErrorWhen(user.Status == 0, 403, "账号已被禁用"); // 条件成立 → 报错
    return user;
}

// Helper / Service 中同样可用
Error(500, "内部错误");
ErrorWhen(isDuplicated, 400, "数据已存在");
ErrorWhenFalse(hasPermission, 403, "无权操作");
```

---

## 5. 控制器规范

### 5.1 基类

所有业务控制器继承 `SimApiBaseController`：

```csharp
using static SimApi.Helpers.SimApiError;

[ApiController]
[Route("[controller]")]
public class UserController(DataContext db) : SimApiBaseController
{
    // 获取当前登录用户（需 EnableSimApiAuth）
    protected SimApiLoginItem LoginInfo => (SimApiLoginItem)HttpContext.Items["LoginInfo"]!;
}
```

`SimApiBaseController` 已内置：
- `[Consumes("application/json")]` + `[Produces("application/json")]`
- `OnActionExecuting` 自动验证 ModelState，无效时抛 `code 400`
- `LoginInfo` 属性获取当前登录信息

### 5.2 路由规则

**默认全部 POST**（除非在 `SupportedMethod` 显式添加）：

```csharp
// 方法上写完整路径
[HttpPost("/device/list")]
[HttpPost("/application/refresh-key")]

// 类上前缀 + 方法相对路径
[Route("/platform")]
public class PlatformController(DataContext db) : SimApiBaseController
{
    [HttpPost("device/detail")]    // 最终路由：/platform/device/detail
    [HttpPost("bot/generate")]     // 最终路由：/platform/bot/generate
}
```

路由路径全小写，多词用连字符 `-` 分隔。

### 5.3 鉴权 Attribute

| Attribute | 用途 |
|-----------|------|
| `[SimApiAuth]` | 要求登录 |
| `[SimApiAuth("admin")]` | 要求 admin 角色 |
| `[SimApiAuth("admin,manager")]` | OR 关系 |
| `[SimApiSign(KeyProvider = typeof(Xxx))]` | 签名验证 |

> 鉴权 Attribute 加在 **Controller 类** 上，不加在方法上。

### 5.4 接口分组与文档

```csharp
// 参数1: 分组Tag（对应 ApiExplorerSettings.GroupName）
// 参数2: 接口名称（必填）
// 参数3: 接口详细描述（可选）
[ApiExplorerSettings(GroupName = "platform")]
[SimApiDoc("设备", "获取设备详情")]
[SimApiDoc("设备", "获取设备详情", "根据设备ID查询设备的完整信息，含关联应用列表")]
[HttpPost("device/detail")]
public Device DeviceDetail(...) { ... }
```

> 每个接口都应标注 `[SimApiDoc]`，至少填写前两个参数。复杂业务接口建议补充第三参数。

### 5.5 返回值规范

| 场景 | 返回类型 | 说明 |
|------|----------|------|
| 写操作（增删改） | `void` | 框架自动返回 `{"code":200}` |
| 单条查询 | 直接返回 Entity | 如 `Account`、`Device` |
| 列表查询 | `Entity[]` 数组 | 不用 `List<T>` |
| 分页查询 | `PageResponse<Entity[]>` | 含 Total/Page/Count/List |
| 有状态响应 | `SimApiBaseResponse` | 自定义 code+message |
| 复杂组合 | 对应 DTO | 自定义结构 |

> **不使用** `ActionResult<T>` 或 `IActionResult`（除非用了 `[AesBody]` 等框架 Attribute）。

### 5.6 参数规范

```csharp
// 普通请求体
[FromBody] DeviceDto.SerialAddRequest request

// AES 加密请求体
[AesBody(KeyProvider = typeof(AesBodyProvider))] BotDto.BotChatRequest request

// 查询参数（直接写，不加 Attribute）
string appId
```

### 5.7 当前登录信息

```csharp
var userId = LoginInfo?.Id;           // 用户唯一标识
var userRole = LoginInfo?.Type;        // string[] 角色列表
```

> **⚠️ 权限检查一律使用 `[SimApiAuth]` Attribute，禁止在代码中手动判断 `LoginInfo.Type.Contains(...)` 来控制权限。**
>
> 需要角色权限的 Controller 直接标注：`[SimApiAuth("admin")]`
> 需要数据归属校验（如"只能操作自己的资源"）在路由方法中直接写条件判断即可。

### 5.8 控制器方法规范

**Controller 中只保留路由方法（即带 `[HttpPost]`/`[HttpGet]` 等路由 Attribute 的 public 方法）。**

不要在 Controller 里写非路由的 private 辅助方法。可复用的横切逻辑应抽取到 `Helpers/` 目录下的独立类中。

```csharp
// ✅ 控制器保持简洁，只有路由方法
[SimApiAuth]
public class UserController(DataContext db) : SimApiBaseController
{
    [HttpPost("list")]
    public User[] GetUserList() => db.Users.OrderBy(x => x.CreatedAt).ToArray();

    [HttpPost("detail")]
    public User GetUser([FromBody] StringIdOnlyRequest req)
    {
        var user = db.Users.Find(req.Id);
        ErrorWhenNull(user);
        return user;
    }
}

// ✅ 复杂逻辑抽到 Helper
// Helpers/UserHelper.cs
public static class UserHelper
{
    public static void ValidateOwnership(DataContext db, string loginId, string resourceId)
    {
        ErrorWhen(!db.Resources.Any(x => x.Id == resourceId && x.OwnerId == loginId),
            403, "无权操作此资源");
    }
}
```

---

## 6. 响应格式详解

### 6.1 结构

```json
{ "code": 200, "message": "成功", "data": { ... } }
```

### 6.2 构造方式

```csharp
// 无数据
return new SimApiBaseResponse();                    // {"code":200,"message":"成功"}
return new SimApiBaseResponse(400, "参数错误");     // {"code":400,"message":"参数错误"}
return new SimApiBaseResponse(404);                // {"code":404,"message":"接口不存在"}

// 带数据
return new SimApiBaseResponse<User>(user);         // data = user

// 分页
return new PageResponse<Device[]>
{
    List = devices, Page = 1, Count = 20, Total = 100
};
```

### 6.3 响应过滤器行为

| 控制器返回值 | 最终 JSON 输出 |
|-------------|--------------|
| `null` | `{"code":200,"message":"成功"}` |
| 普通对象 | `{"code":200,"message":"成功","data":对象}` |
| `SimApiBaseResponse` | 原样输出 |
| `[OriginResponse]` 方法 | 完全不封装，原始输出 |

---

## 7. SimApiOptions 完整配置

通过 `AddSimApi(options => { ... })` 设置：

### 7.1 功能开关

```csharp
options.RedisConfiguration = "localhost:6379";      // 多模块共用
options.EnableSimApiAuth    = false;                // Token 认证
options.EnableSimApiDoc     = false;                // Swagger 文档
options.EnableSimApiStorage = false;                // S3 对象存储
options.EnableJob           = false;                // Hangfire 任务调度
options.EnableSynapse       = false;                // MQTT 通信
options.EnableCoceSdk       = false;                // Coce 统一身份
// 以下默认 true，通常不改：
options.EnableCors                    = true;       // 全量 CORS
options.EnableSimApiException         = true;       // 全局异常拦截
options.EnableSimApiResponseFilter    = true;       // 响应统一封装
options.EnableForwardHeaders          = true;       // 反向代理 Header
options.EnableLowerUrl                = true;       // URL 小写化
options.EnableVersionUrl              = true;       // /versions 接口
options.EnableLogger                  = true;       // 彩色控制台日志
```

### 7.2 子模块配置

```csharp
options.ConfigureSimApiDoc(doc => { ... });        // Swagger
options.ConfigureSimApiStorage(s => { ... });      // S3 存储
options.ConfigureSimApiJob(job => { ... });        // 任务调度
options.ConfigureSimApiSynapse(s => { ... });      // MQTT
options.ConfigureCoceSdk(coce => { ... });         // Coce 身份
```

---

## 8. Attributes 完整参考

### 8.1 [SimApiAuth] — 身份认证

```csharp
[SimApiAuth]                     // 仅检查登录
[SimApiAuth("admin")]            // Type 包含 "admin"
[SimApiAuth("admin,manager")]    // 逗号分隔 OR 关系
[SimApiAuth(new[]{"a", "b"})]    // 数组形式
```

### 8.2 [SimApiDoc] — Swagger 文档注解

```csharp
[SimApiDoc("分组名", "接口名称")]
[SimApiDoc("分组名", "接口名称", "详细描述")]
[SimApiDoc(new[]{"tag1","tag2"}, "接口名称")]
```

### 8.3 [SynapseEvent] — MQTT 事件处理

```csharp
// 参数规则：0个 / 1个(eventName string) / 2个(eventName string, T data)
[SynapseEvent("order/created")]
public void OnOrderCreated(string eventName) { }

[SynapseEvent("order/+/status")]   // 支持 MQTT 通配符 + 和 #
public void OnOrderStatus(string eventName, OrderStatusDto data) { }
```

### 8.4 [SynapseRpc] — MQTT RPC 方法

```csharp
[SynapseRpc]                    // 注册名为 "ClassName.MethodName"
[SynapseRpc("customRpcName")]   // 自定义名

// 支持 0~2 个参数，第2个固定为 Dictionary<string,string>(headers)
public UserDto GetUserInfo(GetUserRequest req) { }
public UserDto GetUserInfo(GetUserRequest req, Dictionary<string, string> headers) { }
```

### 8.5 [AesBody] — AES 解密请求体

```csharp
[HttpPost]
public IActionResult Submit([AesBody(KeyProvider = typeof(MyAesKeyProvider))] MyRequest req)
{ /* request 已自动解密 */ }

// 客户端提交: {"data": "Base64(AES-256-CBC加密JSON)"}
```

### 8.6 [OriginResponse] — 跳过响应封装

```csharp
[HttpGet][OriginResponse]
public string GetRaw() => "raw string";
```

### 8.7 [SimApiSign] — API 签名验证

```csharp
[SimApiSign(KeyProvider = typeof(MySignProvider))]
public IActionResult SecureApi(...) { }
// 签名算法: MD5(field1=v1&...&appId=xxx&timestamp=ts&nonce=nnn&密钥)
```

---

## 9. 模块详解：认证系统（SimApiAuth）

### 9.1 配置

```csharp
options.EnableSimApiAuth = true;   // 必须同时配置 RedisConfiguration
// Token 通过 Header 传入：Token: <value>
```

### 9.2 DI 注入与 API

```csharp
public MyController(SimApiAuth auth) { }

string token = auth.Login(loginItem);              // 自动 GUID token
string token = auth.Login(loginItem, "custom-token");
auth.Update(loginItem, token);                     // 更新不换 token
SimApiLoginItem? info = auth.GetLogin(token);      // 查询登录态
auth.Logout(token);                                // 退出
```

### 9.3 SimApiLoginItem 结构

```csharp
{ Id: string, Type: string[], Meta: Dictionary<string,string>, Extra: object? }
```

- `Id`: 用户唯一标识
- `Type`: 角色数组，如 `["user", "admin"]`
- `Meta`: 附加元数据字典
- `Extra`: 扩展对象

### 9.4 自动路由

启用后自动生成：

| 路由 | 方法 | 说明 |
|------|------|------|
| `POST /auth/check` | 无需登录 | 检测登录状态，返回用户 ID |
| `POST /auth/logout` | 无需登录 | 退出登录 |
| `POST /user/info` | 需要登录 | 获取当前用户完整信息 |

---

## 10. 模块详解：Swagger 文档（EnableSimApiDoc）

### 10.1 配置

```csharp
options.ConfigureSimApiDoc(doc =>
{
    doc.DocumentTitle = "接口文档";

    doc.ApiGroups = [
        new("api",   "公共接口"),
        new("admin", "管理接口", "描述可选")
    ];

    doc.ApiAuth = new SimApiAuthOption { Type = ["SimApiAuth"] };
    doc.SupportedMethod = [SubmitMethod.Post]; // 默认仅 Post！按需添加其他
});

// 分组方式
[ApiExplorerSettings(GroupName = "admin")]   // 归入 admin 组
// 不标注则默认归入 "api" 组
```

### 10.2 访问

启动后访问 `/swagger`。

### 10.3 自动 Swagger 过滤器（无需手动配置）

| 过滤器 | 效果 |
|--------|------|
| `SimApiResponseOperationFilter` | 返回类型包装为 `SimApiBaseResponse<T>` |
| `SimApiAuthOperationFilter` | `[SimApiAuth]` 接口加 Token 认证要求 |
| `SimApiSignOperationFilter` | `[SimApiSign]` 接口注入签名参数说明 |
| `AesBodyOperationFilter` | `[AesBody]` 参数展示原始数据结构 |
| `GlobalDynamicObjectSchemaFilter` | `object`/`Dictionary` 类型生成 Schema 示例 |
| `RemoveEmptyTagsFilter` | 清除空分组 Tag |

---

## 11. 模块详解：对象存储（EnableSimApiStorage）

基于 MinIO SDK（S3 兼容）。

### 11.1 配置

```csharp
options.EnableSimApiStorage = true;
options.ConfigureSimApiStorage(s =>
{
    s.Endpoint  = "http://minio:9000";   // 不能以 / 结尾
    s.AccessKey = "admin";
    s.SecretKey = "pass";
    s.Bucket    = "my-bucket";
    s.ServeUrl  = "http://cdn.example.com/my-bucket"; // 不能以 / 结尾
});
```

### 11.2 API

```csharp
public MyController(SimApiStorage storage) { }

// 路径必须以 / 开头
GetUploadUrlResponse r = storage.GetUploadUrl("/avatars/user1.jpg");
// r.UploadUrl   → PUT 上传地址（前端直接用）
// r.DownloadUrl → 公开访问 URL
// r.Path        → 相对路径

string url = storage.GetDownloadUrl("/files/doc.pdf");            // 默认 10 分钟
string url = storage.GetDownloadUrl("/files/doc.pdf", expire: 3600);

storage.UploadFile("/path/file.jpg", stream, "image/jpeg");

string? url  = storage.FullUrl("/path/file");   // 路径转完整 URL
string? url  = storage.GetUrl("/path/file");     // 同上
string? path = storage.GetPath("http://cdn.../my-bucket/path/file"); // URL→路径

IMinioClient mc = storage.Client;  // 底层 MinIO 客户端
```

---

## 12. 模块详解：Redis 缓存（SimApiCache）

> 依赖 `RedisConfiguration`，key 自动加前缀 `SimApi:Cache:`

```csharp
public MyService(SimApiCache cache) { }

cache.Set("key", value);                                              // 永不过期
cache.Set("key", value, new DistributedCacheEntryOptions {
    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
});
string? raw = cache.Get("key");                                       // 原始字符串
int? val    = cache.Get<int>("key");                                  // 反序列化
cache.Remove("key");                                                  // 删除
```

---

## 13. 模块详解：任务调度（EnableJob）

Hangfire + Redis 存储。

### 13.1 配置

```csharp
options.EnableJob = true;
options.ConfigureSimApiJob(job =>
{
    job.DashboardUrl      = "/jobs";     // null 则不开启 Dashboard
    job.DashboardAuthUser = "admin";
    job.DashboardAuthPass = "Admin@123!";
    job.RedisConfiguration = null;       // null 则用全局 RedisConfiguration
    job.Database = 1;                    // Redis DB 编号
    job.Servers = [
        new SimApiJobServerConfig { Queues = ["default"], WorkerNum = 5 },
        new SimApiJobServerConfig { Queues = ["email"],   WorkerNum = 2 }
    ];
});
```

### 13.2 使用

```csharp
BackgroundJob.Enqueue(() => myService.DoWork());                        // 立即执行
BackgroundJob.Schedule(() => myService.DoWork(), TimeSpan.FromMinutes(5)); // 延迟
RecurringJob.AddOrUpdate("job-id", () => myService.DoWork(), Cron.Daily);   // 定时
var id = BackgroundJob.Enqueue(() => Step1());
BackgroundJob.ContinueJobWith(id, () => Step2());                           // 依赖链
```

Dashboard 访问 `/jobs`，Basic Auth 登录。

---

## 14. 模块详解：MQTT 通信（EnableSynapse）

基于 MQTTnet v5，WebSocket 连接。

### 14.1 配置

```csharp
options.EnableSynapse = true;
options.ConfigureSimApiSynapse(s =>
{
    s.Websocket = "ws://mqtt:8083/mqtt";
    s.Username  = "user";
    s.Password  = "pass";
    s.SysName   = "my-system";           // Topic 命名空间前缀
    s.AppName   = "order-service";       // 服务名
    s.AppId     = "instance-001";        // 实例ID（不填自动GUID）
    s.RpcTimeout = 3;                    // RPC 超时秒数
    s.EventLoadBalancing  = false;       // $queue 订阅负载均衡
    s.EnableConfigStore   = true;        // 分布式配置中心
    s.DisableEventClient  = false;
    s.DisableRpcClient    = false;
});
```

### 14.2 Topic 规则

| 用途 | Topic 格式 |
|------|-----------|
| 事件发布 | `{SysName}/event/{AppName}/{eventName}` |
| 事件订阅（无负载均衡） | `{SysName}/event/{eventName}` |
| 事件订阅（有负载均衡） | `$queue/{SysName}/event/{eventName}` |
| RPC 请求 | `{SysName}/{targetApp}/rpc/server/{method}`（$queue 天然负载均衡） |
| RPC 响应 | `{SysName}/{callerApp}/rpc/client/{AppId}/{messageId}` |
| 配置存储 | `{SysName}/synapse-config-store/{key}`（Retain 消息） |

### 14.3 Synapse API

```csharp
public MyService(Synapse synapse) { }

synapse.Event("order/created", new { OrderId = 1 });  // 发布事件

// RPC 同步调用，返回 SimApiBaseResponse<T>
var res = synapse.Rpc<UserDto>("user-service", "GetUserInfo", new { Id = 1 });
var res = synapse.Rpc<UserDto>("user-service", "GetUserInfo", param,
    headers: new Dictionary<string, string> { { "traceId", "xxx" } });

// code=502 表示 RPC 超时

// 分布式配置
synapse.SetConfig("key", "value");
string? val = synapse.GetConfig("key");
synapse.OnConfigChanged += (sender, item) => Console.WriteLine($"{item.Key}={item.Value}");

// RPC 方法内部抛错
synapse.RpcError(400, "参数错误");
synapse.RpcErrorWhen(id <= 0, 400, "ID 无效");
```

### 14.4 处理器注册

含 `[SynapseRpc]`/`[SynapseEvent]` 的类会被**自动扫描注册为 Scoped 服务**，无需手动注册：

```csharp
// 事件处理器
public class OrderEventHandler
{
    [SynapseEvent("order/+/status")]
    public void OnOrderStatus(string eventName, OrderStatusDto data) { }
}

// RPC 服务
public class UserRpcService
{
    [SynapseRpc]  // 注册为 "UserRpcService.GetUserInfo"
    public UserDto GetUserInfo(GetUserRequest req) { return ...; }

    [SynapseRpc("customName")]
    public ResultDto DoSomething(RequestDto req, Dictionary<string, string> headers) { }
}
```

---

## 15. 模块详解：API 签名验证（[SimApiSign]）

### 15.1 实现密钥提供者

```csharp
public class MySignProvider : SimApiSignProviderBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    public MySignProvider(IServiceScopeFactory sf) => _scopeFactory = sf;

    // ── 以下参数均有默认值，按需覆盖即可（不写则用默认值）──
    public override string? AppIdName   { get; set; } = "appId";           // Query/Header 参数名：应用ID
    public override string TimestampName { get; set; } = "timestamp";      // Query/Header 参数名：时间戳
    public override string NonceName    { get; set; } = "nonce";           // Query/Header 参数名：随机串
    public override string SignName     { get; set; } = "sign";            // Query/Header 参数名：签名值
    public override int    QueryExpires { get; set; } = 5;                 // 签名有效期（秒）
    public override bool   DuplicateRequestProtection { get; set; } = true;// 防重放攻击
    public override string[] SignFields { get; set; } = [];                // 额外参与签名的业务字段（默认无）

    // ── 必须实现：根据 appId 返回对应密钥 ──
    public override string? GetKey(string? appId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        return db.Apps.Find(appId)?.SecretKey;
    }
}

builder.Services.AddScoped<MySignProvider>();
```

### 15.2 使用

```csharp
[SimApiSign(KeyProvider = typeof(MySignProvider))]
public IActionResult SecureApi(...) { }

// 签名算法: MD5(field1=v1&field2=v2&...&appId=xxx&timestamp=ts&nonce=nnn&密钥)
// 支持通过 Query 或 Header 传入签名参数
```

---

## 16. 模块详解：AES 加密传输（[AesBody]）

算法：**AES-256-CBC + PKCS7**，IV 随机生成附在密文前，整体 Base64 编码。

### 16.1 实现密钥提供者

```csharp
public class MyAesKeyProvider : AesBodyProviderBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    public MyAesKeyProvider(IServiceScopeFactory sf) => _scopeFactory = sf;

    public override string? AppIdName { get; set; } = "appId";

    public override string? GetKey(string? appId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        return db.Apps.Find(appId)?.SecretKey;
    }
}
builder.Services.AddScoped<MyAesKeyProvider>();
```

### 16.2 使用

```csharp
[HttpPost]
public IActionResult Submit([AesBody(KeyProvider = typeof(MyAesKeyProvider))] MyRequest req)
{ /* request 已解密反序列化 */ }

// 客户端提交: {"data": "Base64(AES-256-CBC 密文)"}

// 静态工具类（无需注入，任意长度密钥会经 SHA256 处理为32字节）
string cipher = SimApiAesUtil.Encrypt("明文", "任意长度密钥");
string plain  = SimApiAesUtil.Decrypt(cipher, "任意长度密钥");
```

---

## 17. 模块详解：HTTP 客户端（SimApiHttpClient）

用于调用其他带签名/AES 加密的 SimApi 服务。

### 17.1 构造函数与属性

```csharp
// 构造函数（必填参数）
var client = new SimApiHttpClient(
    appId: "myapp",    // 必填：应用 ID
    appKey: "secret"   // 必填：应用密钥
    // debug: false     // 可选：是否打印请求/响应日志（默认 false）
);

// 以下属性均有默认值，按需覆盖即可：
client.Server        = "https://api.example.com"; // 必须设置！目标服务地址（无默认值）
client.AppIdName     = "appId";                   // 默认 "appId"
client.TimestampName = "timestamp";               // 默认 "timestamp"
client.NonceName     = "nonce";                   // 默认 "nonce"
client.SignName      = "sign";                    // 默认 "sign"
client.SignFields    = ["field1", "field2"];      // 默认 []（空）
```

> **只有 `Server` 是必须设置的**，其他属性都有合理默认值。

### 17.2 调用方法

```csharp
// 仅签名（自动计算 MD5 签名附加到 Query/Header）
var r = client.SignQuery<T>("/api/user", body, queries);

// 仅 AES 加密（body 自动加密为 {"data":"Base64密文"}）
var r = client.AesQuery<T>("/api/user", body);

// AES 加密 + 签名
var r = client.AesSignQuery<T>("/api/user", body, queries);
```

---

## 18. 模块详解：Coce 统一身份平台（EnableCoceSdk）

> 同时需要 `EnableSimApiAuth = true`

### 18.1 配置

```csharp
options.ConfigureCoceSdk(coce =>
{
    coce.ApiEndpoint  = "https://api.coce.cc";   // 默认
    coce.AuthEndpoint = "https://home.coce.cc";  // 默认
    coce.AppId  = "your-app-id";
    coce.AppKey = "your-app-key";
});
```

### 18.2 CoceApp API

```csharp
public MyService(CoceApp coce) { }

// 用户
coce.GetUserInfo(levelToken)
coce.GetUserGroups(levelToken)
coce.SearchUserByPhone("13800138000")
coce.SearchUserByIds(new[]{"uid1","uid2"})

// 消息
coce.SendUserMessage(userId, "标题", "内容")

// 支付
string? tradeNo = coce.TradeCreate("商品名", 100, "扩展数据")
coce.TradeCheck(tradeNo)
coce.TradeRefund(tradeNo)

// Token
coce.GetLevelToken(lv1Token, level: 5)
coce.SaveToken(userId, levelToken)
coce.GetToken(userId)

// 代理请求
coce.ProxyQuery<T>(uri, token)
coce.ProxyQueue<T>(uri, token, data)
```

### 18.3 自动路由

| 路由 | 方法 | 说明 |
|------|------|------|
| `POST /auth/login` | 无需登录 | Coce 一键登录（前端传 `{"data":"lv1Token"}`） |
| `POST /user/groups` | 需登录 | 获取群组列表 |
| `GET /auth/config` | 无需登录 | 获取 AppId 和授权 URL |

### 18.4 自定义登录处理器

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
builder.Services.AddScoped<ICoceLoginProcessor, MyLoginProcessor>();
```

---

## 19. 工具类（SimApiUtil，全部静态）

```csharp
DateTime cst    = SimApiUtil.CstNow;          // UTC+8 当前时间
double   ts     = SimApiUtil.TimestampNow;     // 秒级 Unix 时间戳
string   simVer = SimApiUtil.SimApiVersion;    // SimApi 包版本
string   appVer = SimApiUtil.AppVersion;       // 宿主应用版本

string md5  = SimApiUtil.Md5("src");           // 32位 MD5
string md5  = SimApiUtil.Md5("src", "x3");     // 48位
string sha1 = SimApiUtil.Sha1("src");

string json = SimApiUtil.Json(obj);             // camelCase，中文不转义
T obj       = SimApiUtil.XmlDeserialize<T>(xml);
JsonSerializerOptions opts = SimApiUtil.JsonOption; // 可复用配置

bool ok = SimApiUtil.CheckCell("13800138000"); // 手机号验证

// IQueryable 分页扩展
var paged = dbContext.Users.AsQueryable().Paginate(page: 1, count: 20);
```

---

## 20. 数据模型基类（SimApiBaseModel）

ORM 实体基类，提供通用字段和轻量映射能力：

```csharp
public class UserEntity : SimApiBaseModel
{
    public string Name { get; set; }
    // 自动拥有: Id(GUID string)、CreatedAt、UpdatedAt
}

entity.MapData(dto);                      // 跳过 Id/CreatedAt/UpdatedAt，同名同类型非null属性
entity.MapData(dto, mapAll: true);        // 映射所有字段
entity.MapData(dto, new[]{"Name"});       // 只映射指定字段
entity.UpdateTime();                      // 手动更新 UpdatedAt

// MapData 只映射: 同名 + 同类型 + 源值不为null
```

---

## 21. DTO 规范

### 21.1 组织方式

DTO 在 `Controllers/Dtos/` 下，嵌套容器类：

```csharp
namespace MyApp.Controllers.Dtos;

public abstract class AdminDto
{
    public class UserEditRequest
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
    }

    public class ApplicationListRequest : SimApiBasePageRequest
    {
        public string? Keyword { get; set; }
    }
}
```

### 21.2 命名规则

| 类型 | 格式 | 示例 |
|------|------|------|
| 请求 DTO | `[动作]Request` | `UserEditRequest`、`DeviceSerialAddRequest` |
| 响应 DTO | `[动作]Response` | `GenerateResponse`、`TokenResponse` |
| 数据载体 | `[含义]Data` / `[含义]Item` | `GenerateData`、`AgentItem` |

引用时用全限定名：`AdminDto.UserEditRequest`。

### 21.3 属性规则

```csharp
public class RequestDto
{
    public required string Verify { get; set; }       // 必填
    public required string AppId { get; set; }
    [Range(1, 10000)] public required int Num { get; set; }  // 范围校验
    public string? Remark { get; set; }               // 可选
    public int Status { get; set; } = 1;              // 有默认值
}
```

### 21.4 框架内置 DTO（优先复用）

| DTO | 用途 |
|-----|------|
| `SimApiStringIdOnlyRequest` | 只有 `Id` 字段 |
| `SimApiOneFieldRequest<T>` | 只有 `Data` 字段 |
| `SimApiBasePageRequest` | 分页请求基类（Page + Count） |
| `SimApiBaseResponse` | 通用响应（可带 code + message） |
| `SimApiBaseResponse<T>` | 带数据的响应 |
| `PageResponse<T>` | 分页响应（Total + Page + Count + List） |

---

## 22. Entity 与 DataContext 规范

### 22.1 Entity

所有实体继承 `SimApiBaseModel`：

```csharp
public class Account : SimApiBaseModel
{
    public required string Name { get; set; }
    public required string Username { get; set; }
    public required string Role { get; set; } = "user";
    public int Status { get; set; } = 1;
}
```

- 必填用 `required`，可选用 `?`，有默认值直接赋值
- 外键命名：`[关联实体]Id`，如 `AccountId`、`AppId`
- **不配导航属性**，**不写 Fluent API**，依赖 Convention 自动映射

### 22.2 DataContext

只定义 DbSet，不做任何配置：

```csharp
public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
{
    public required DbSet<Account> Accounts { get; set; }
    public required DbSet<Application> Applications { get; set; }
}
```

---

## 23. EF Core 查询风格

```csharp
// 列表查询（排序 + ToArray）
db.Accounts.OrderBy(x => x.CreatedAt).ToArray();

// 动态条件查询
var query = db.Devices.Where(x => x.ApplicationId == appId).OrderBy(x => x.CreatedAt).AsQueryable();
if (!string.IsNullOrEmpty(request.Serial))
    query = query.Where(x => x.Serial == request.Serial);

// 分页
var list = query.Paginate(request.Page, request.Count).ToArray();
var total = query.Count();

// 单条查询
db.Accounts.Find(id);                                      // 主键用 Find（命中缓存）
db.Accounts.FirstOrDefault(x => x.Username == username);    // 其他用 FirstOrDefault

// 写操作
db.Add(entity);       // 新增
db.Update(entity);    // 修改
db.Remove(entity);    // 删除
db.SaveChanges();     // 最后统一 SaveChanges 一次

// 存在性判断（不用 Count > 0）
db.AppServices.Any(x => x.ServiceId == request.Id)
```

---

## 24. Program.cs 完整模板

```csharp
var builder = WebApplication.CreateBuilder(args);

// 1. SimApi 框架
builder.Services.AddSimApi(options =>
{
    options.RedisConfiguration = builder.Configuration.GetConnectionString("Redis");
    options.EnableSimApiAuth = true;
    options.EnableSimApiDoc = true;
    options.EnableSimApiStorage = false;
    options.EnableJob = false;
    options.EnableSynapse = false;

    options.ConfigureSimApiDoc(doc =>
    {
        doc.DocumentTitle = "接口文档";
        doc.ApiGroups = [new("api", "公共接口"), new("admin", "管理接口")];
        doc.SupportedMethod = [SubmitMethod.Post];
    });
});

// 2. 数据库
builder.Services.AddDbContext<DataContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// 3. 框架扩展点（接口注册）
builder.Services.AddScoped<AesBodyProviderBase, AesBodyProvider>();
builder.Services.AddScoped<SimApiSignProviderBase, SimApiSignProvider>();

// 4. 项目自定义服务
builder.Services.AddScoped<ResPermission>();
builder.Services.AddSingleton<JsonSchemaHelper>();

var app = builder.Build();

// 5. 启动时自动迁移
app.Services.CreateScope().ServiceProvider
    .GetRequiredService<DataContext>().Database.Migrate();

// 6. 框架中间件
app.UseSimApi();
app.Run();
```

---

## 25. 配置文件规范

`appsettings.json` 只保留框架默认值：

```json
{
  "Logging": { "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" } },
  "AllowedHosts": "*"
}
```

`appsettings.Development.json` 存放开发环境实际配置（不提交 Git）：

```json
{
  "ConnectionStrings": {
    "Default": "Host=...;Database=...;Username=...;Password=...",
    "Redis": "host:port,defaultDatabase=N"
  },
  "Sms": { "Account": "...", "Password": "..." }
}
```

读取方式：
```csharp
builder.Configuration.GetConnectionString("Default")
config["Gateway:Key"]
config.GetSection("Sms").GetSection("Templates")["verify"]
```

---

## 26. 内置路由汇总

| 路由 | 方法 | 启用条件 |
|------|------|----------|
| `/swagger` | GET | `EnableSimApiDoc` |
| `/versions` | GET/POST | `EnableVersionUrl`（默认开） |
| `/auth/check` | POST | `EnableSimApiAuth` |
| `/auth/logout` | POST | `EnableSimApiAuth` |
| `/user/info` | POST | `EnableSimApiAuth`（需登录） |
| `/auth/login` | POST | `EnableCoceSdk` |
| `/user/groups` | POST | `EnableCoceSdk`（需登录） |
| `/auth/config` | GET | `EnableCoceSdk` |
| `/jobs` | GET | `EnableJob` |

---

## 27. 注释规范

- **公有 API/方法**：XML 文档注释
- **私有方法**：简单可不写；复杂逻辑写行内注释说**为什么**
- **不要废话注释**

```csharp
/// <summary>
/// 根据邮箱查用户，不存在返回 null。
/// </summary>
public Account? FindByEmail(string email) => db.Accounts.FirstOrDefault(x => x.Email == email);

// ✅ 有意义的注释（解释原因）
// EF Core Find 优先命中一级缓存
var user = db.Accounts.Find(id);

// ❌ 废话注释
// 查询用户
var user = db.Accounts.Find(id);
```

---

## 28. 禁止事项

以下模式在使用 SimApi 框架时**明确禁止**：

| ❌ 禁止 | ✅ 正确做法 |
|---------|------------|
| HTTP 4xx/5xx 表达业务错误 | HTTP 200 + JSON `code` 字段 |
| `throw new Exception(message)` | `ErrorWhen` 系列 或 `throw new SimApiException(code, msg)` |
| 新建 Service / Repository 层（除非项目明确需要） | Controller 直接操作 DbContext |
| 使用 `ActionResult<T>` / `IActionResult` | 直接返回 Entity / void / SimApiBaseResponse |
| 鉴权 Attribute 加在方法上 | 加在 Controller **类** 上 |
| **手动判断 `LoginInfo.Type.Contains(...)` 做权限控制** | **一律用 `[SimApiAuth("role")]` Attribute** |
| **Controller 里写非路由的 private 辅助方法** | **抽到 `Helpers/` 独立类** |
| Entity 配导航属性 / EF Fluent API | 依赖 Convention 自动映射 |
| DbContext 中写 `OnModelCreating`（除非必要） | 只定义 DbSet |
| 花括号块命名空间 | 文件范围命名空间 |
| 传统构造函数注入 | 主构造函数 |
| `new List<T>()` / `new string[] {}` | `[]` 集合表达式 |
| `Count() > 0` 判断存在 | `Any()` |
| `ToList()` 再转数组 | 直接 `ToArray()` |
| `string.IsNullOrEmpty` 判断必填入参 | `required` + 模型验证 |
| 全局 catch 吞异常 | 让异常冒泡到 SimApiExceptionMiddleware |
| `SimApiStorageOptions = Configuration.GetSection(...)` | `ConfigureSimApiStorage(s => {...})` |
