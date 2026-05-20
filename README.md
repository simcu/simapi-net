# SimApi for .NET

> NuGet: `Simcu.SimApi` | 目标框架: `net8.0` / `net9.0` / `net10.0`  
> 作者: xRain@SimcuTeam  

ASP.NET Core API 基础框架库，提供统一异常拦截、响应封装、Token 认证、Swagger 文档、S3 存储、MQTT 通信、Hangfire 任务调度、Auth Center 网关鉴权与 IAM 权限管理。

---

## 快速开始

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSimApi(options =>
{
    options.RedisConfiguration = "localhost:6379";
    options.EnableSimApiAuth = true;
    options.EnableSimApiDoc = true;

    options.ConfigureSimApiDoc(doc =>
    {
        doc.DocumentTitle = "我的API";
        doc.ApiGroups = [new("api", "公共接口"), new("admin", "管理接口")];
    });
});

var app = builder.Build();
app.UseSimApi();
app.Run();
```

---

## 核心概念

### 统一响应格式

所有接口输出 JSON，HTTP 状态码始终 `200`，错误信息在 `code` 字段：

| code | 含义 |
|------|------|
| 200 | 成功 |
| 204 | 无数据 |
| 400 | 参数错误 |
| 401 | 需要登录 |
| 403 | 无权访问 |
| 404 | 资源不存在 |
| 500 | 服务器错误 |

### 异常处理流程

```
请求 → SimApiExceptionMiddleware(全异常捕获→HTTP 200+JSON)
     → SimApiAuthMiddleware(Token→LoginInfo)
     → [SimApiSign] Filter(签名)
     → [SimApiAuth] Filter(登录检查)
     → OnActionExecuting(模型验证→code 400)
     → Action
     → SimApiResponseFilter(封装响应)
```

---

## 项目结构

```
SimApi/
├── Attributes/            # [SimApiAuth] [SimApiDoc] [SimApiSign] [AesBody] [SynapseEvent] [SynapseRpc] [OriginResponse]
├── AuthSDK/               # Auth Center & IAM SDK
│   ├── SimApiAuthClient.cs             HTTP 客户端（签名调用 Auth Center）
│   ├── SimApiAuthCenter.cs             Auth Center API 封装
│   ├── SimApiAuthCenterDto.cs          数据模型
│   ├── SimApiAuthCenterMiddleware.cs   网关鉴权中间件
│   ├── SimApiAuthIam.cs                IAM 权限管理 API
│   └── SimApiAuthIamDto.cs             IAM 数据模型
├── Communications/        # SimApiBaseResponse, PageResponse<T>, SimApiLoginItem, SimApiBaseRequest
├── Configurations/        # SimApiOptions + 各模块 Option 类
├── Controllers/           # SimApiBaseController, SimApiCommonController, SimApiAuthController
├── Helpers/               # SimApiError, SimApiAuth, SimApiCache, SimApiHttpClient, SimApiStorage, SimApiUtil, SimApiAesUtil
├── Interfaces/            # ISimApiAuthChecker
├── Middlewares/            # SimApiExceptionMiddleware, SimApiAuthMiddleware
├── Synapse/               # MQTT Pub/Sub + RPC + Config Store
├── Exceptions/            # SimApiException
├── Models/                # SimApiBaseModel
├── SwaggerFilters/        # 文档自动过滤器
├── ModelBinders/          # AesBody, SimApiSign
├── Logger/                # 彩色控制台日志
└── SimApiExtensions.cs    # AddSimApi + UseSimApi
```

---

## 1. 错误处理 — SimApiError

独立静态类 `SimApi.Helpers.SimApiError`。所有错误最终 → `throw new SimApiException(code, message)` → 中间件捕获。

**使用**: 文件顶部加 `using static SimApi.Helpers.SimApiError;`

### 完整方法签名

```csharp
// 直接抛错
void Error(int code = 500, string message = "");

// condition 为 true 时抛错 (code 默认 400)
void ErrorWhen([DoesNotReturnIf(true)] bool condition, int code = 400, string message = "");

// 同上（别名）
void ErrorWhenTrue([DoesNotReturnIf(true)] bool condition, int code = 400, string message = "");

// condition 为 false 时抛错
void ErrorWhenFalse([DoesNotReturnIf(false)] bool condition, int code = 400, string message = "");

// obj 为 null 时抛错 (code 默认 404)
void ErrorWhenNull([NotNull] object? condition, int code = 404, string message = "");
```

### 示例

```csharp
using static SimApi.Helpers.SimApiError;

var user = db.Users.Find(id);
ErrorWhenNull(user, 404, "用户不存在");
ErrorWhen(amount <= 0, 400, "金额无效");
ErrorWhenFalse(hasPermission, 403, "无权操作");
Error(500, "服务器内部错误");
```

---

## 2. 控制器 — SimApiBaseController

```csharp
public class SimApiBaseController : Controller
{
    // 当前登录信息（需 EnableSimApiAuth）
    protected SimApiLoginItem LoginInfo => (SimApiLoginItem)HttpContext.Items["LoginInfo"]!;
    protected string LoginToken => (string)HttpContext.Items["LoginToken"]!;

    // OnActionExecuting 自动验证 ModelState → 无效时抛 code 400
}
```

**内建**: `[Consumes("application/json")]` + `[Produces("application/json")]`

### 自动路由

| 路由 | 方法 | 条件 | 说明 |
|------|------|------|------|
| `/versions` | GET/POST | `EnableVersionUrl`(默认) | 返回 SimApi/App 版本 |
| `/user/info` | POST | `EnableSimApiAuth` | 需登录，返回 LoginInfo |
| `/logout` | POST | `EnableSimApiAuth` | 退出登录（可自定义路由） |
| `/swagger` | GET | `EnableSimApiDoc` | Swagger UI |
| `/jobs` | GET | `EnableJob` + DashboardUrl | Hangfire 控制台 |
| `/exception/{code:int}` | GET | 始终 | 错误反馈页面 |

### 返回值规范

| 场景 | 返回类型 |
|------|----------|
| 写操作 | `void` |
| 单条查询 | 直接 Entity |
| 列表查询 | `Entity[]` |
| 分页 | `PageResponse<Entity[]>` |
| 自定义状态 | `SimApiBaseResponse` |
| 跳封装 | 方法加 `[OriginResponse]` |

---

## 3. 认证系统 — SimApiAuth

```csharp
string Login(SimApiLoginItem loginItem, TimeSpan? expireTime = null, string? token = null);  // 默认7天
string Update(SimApiLoginItem loginItem, string token);
SimApiLoginItem? GetLogin(string token);
SimApiLoginItem[] GetAllLogins(string userId);
void Logout(string token);
void LogoutAll(string userId);
```

```csharp
// LoginItem 结构
public class SimApiLoginItem {
    required string Id;
    string[] Type = ["user"];
    Dictionary<string, string> Meta = [];
    Dictionary<string, object?> Extra = [];
}
```

**Token 传参**: Header `Token: <value>`

### 认证后处理 Hook — ISimApiAuthChecker

```csharp
public interface ISimApiAuthChecker {
    void Run(SimApiLoginItem loginItem, string token);
}
// 实现后自动注册为 Scoped，每次认证后调用
```

---

## 4. AuthSDK — Auth Center 网关 & IAM

启用: `EnableSimApiAuthGate = true`

### 4.1 配置

```csharp
options.EnableSimApiAuthGate = true;
options.ConfigureSimApiAuthCenter(gate =>
{
    gate.Server  = "https://auth.coce.cc";
    gate.AppId   = "your-app-id";
    gate.AppKey  = "your-app-key";
    gate.UseMiddleware = true;   // 内部应用启用，解析 X-SimApi-Gate-Auth Header
    gate.UseIam   = true;        // 启用 IAM
});
```

### 4.2 SimApiAuthClient

继承 `SimApiHttpClient`，自动配置 Server/AppId/AppKey，对 Auth Center 发起签名请求。

### 4.3 SimApiAuthCenter — Auth Center API

```csharp
// === 签名验证 ===
void VerifySign(string appId, string timestamp, string nonce, string sign);

// === 群组 ===
GroupRelatedItem[]? GroupRelated(string profileId);
AppAndProfileItem[]? GroupSearch(string keyword, int skip = 0, int take = 20);
GroupDetailTreeNode? GroupDetail(string groupId, string profileId);
string[]? GroupRelatedIndex(string groupId, string profileId);

// === Profile ===
AppAndProfileItem[]? ProfileSearch(string keyword, int skip = 0, int take = 20);
AppAndProfileItem[]? ProfileList(string[] ids);

// === 内部应用专用 ===
bool CheckIsAppOwner(string profileId, string applicationId);
AppAndProfileItem[]? GetAppList(string profileId, IEnumerable<string> appIds);

// === 登录 ===
GetCodeResponse GetLoginCode(string? scene = null, Dictionary<string, object>? data = null, string? backUrl = null);
LoginInfoResponse GetLoginInfo(string code, string? scene = null);

// === 安全验证（二次确认） ===
GetCodeResponse GetConfirmCode(string scene, string userId, Dictionary<string, object>? data = null, string? backUrl = null);
ConfirmResponse Confirm(string code, string scene, string? userId = null);
```

**GetCodeResponse**: `{ Code, Server, FullUrl }`

### 4.4 SimApiAuthIam — IAM 权限管理

```csharp
void RegisterPermissions(PermissionItem[] permissions);
string[] GetPermissionOwned(string profileId);
void CheckPermission(string profileId, string permission);
```

**PermissionItem**: `{ Identifier, Name, Group, Description }`

### 4.5 SimApiAuthCenterMiddleware — 网关鉴权

内部应用专用。解析请求头 `X-SimApi-Gate-Auth` / `X-SimApi-Gate-Time` / `X-SimApi-Gate-Sign`，MD5 验签后将 Base64 解码的用户信息注入 `HttpContext.Items["LoginInfo"]`。

---

## 5. Attributes 完整参考

### [SimApiAuth] — 身份认证

```csharp
[SimApiAuth]                    // 仅检查登录
[SimApiAuth("admin")]           // 单角色
[SimApiAuth("admin,manager")]   // 逗号分隔 OR 关系
[SimApiAuth(new[]{"a","b"})]    // 数组形式
```
- 加在 **Controller 类** 或 **方法** 上
- **禁止** 代码中手动判断 `LoginInfo.Type.Contains(...)` 做权限控制

### [SimApiDoc] — Swagger 文档注解

```csharp
[SimApiDoc("分组名", "接口名称")]
[SimApiDoc("分组名", "接口名称", "详细描述")]
[SimApiDoc(new[]{"tag1","tag2"}, "接口名称")]
```

### [SimApiSign] — API 签名验证

```csharp
[SimApiSign(KeyProvider = typeof(MySignProvider))]
// 签名: MD5(field1=v1&...&appId=xxx&timestamp=ts&nonce=nnn&密钥)
```

实现 `SimApiSignProviderBase`：
```csharp
public class MySignProvider : SimApiSignProviderBase
{
    // 可覆盖: AppIdName, TimestampName, NonceName, SignName, QueryExpires(秒), DuplicateRequestProtection, SignFields
    public override string? GetKey(string? appId) { ... }
}
```

### [AesBody] — AES 解密请求体

```csharp
[AesBody(KeyProvider = typeof(MyAesKeyProvider))] MyRequest req
// 客户端提交: {"data": "Base64(AES-256-CBC 密文)"}
```

实现 `AesBodyProviderBase`，覆盖 `AppIdName` 和 `GetKey(appId)`。

### [SynapseEvent] — MQTT 事件处理

```csharp
[SynapseEvent("order/created")]          // 指定 eventName
[SynapseEvent]                            // 不指定 = 方法名
// 参数: 0个 / 1个(string eventName) / 2个(string eventName, T data)
```

### [SynapseRpc] — MQTT RPC 方法

```csharp
[SynapseRpc]                              // 注册名 "ClassName.MethodName"
[SynapseRpc("customName")]                // 自定义名
// 参数: 0~2个，第2个固定 Dictionary<string,string>(headers)
```

### [OriginResponse] — 跳过响应封装

```csharp
[HttpGet][OriginResponse]
public string GetRaw() => "raw";
```

---

## 6. Swagger 文档 — EnableSimApiDoc

```csharp
options.ConfigureSimApiDoc(doc =>
{
    doc.DocumentTitle = "接口文档";
    doc.ApiGroups = [new("api", "公共"), new("admin", "管理", "描述可选")];
    doc.SupportedMethod = [SubmitMethod.Post];       // 默认仅POST
    doc.ApiAuth = new SimApiAuthOption { Type = ["SimApiAuth"] };  // 认证方式
});
```

每组通过 `[ApiExplorerSettings(GroupName = "admin")]` 分类。

### 自动过滤器

| 过滤器 | 效果 |
|--------|------|
| `SimApiResponseOperationFilter` | 返回值包装为 `SimApiBaseResponse<T>` |
| `SimApiAuthOperationFilter` | 鉴权接口 + Token Header |
| `SimApiSignOperationFilter` | 签名接口注入签名参数 |
| `AesBodyOperationFilter` | AES 接口展示原始结构 |
| `GlobalDynamicObjectSchemaFilter` | object/Dictionary → Schema |
| `RemoveEmptyTagsFilter` | 清除空分组 |

---

## 7. 对象存储 — EnableSimApiStorage

基于 MinIO SDK（S3 兼容）。

### 配置

```csharp
options.EnableSimApiStorage = true;
options.ConfigureSimApiStorage(s =>
{
    s.Endpoint  = "http://minio:9000";    // 不能以 / 结尾
    s.AccessKey = "admin";
    s.SecretKey = "pass";
    s.Bucket    = "bucket";
    s.ServeUrl  = "http://cdn.example.com/bucket";  // 不能以 / 结尾
});
```

### API

```csharp
GetUploadUrlResponse GetUploadUrl(string path, int expire = 7200);
// 返回: { UploadUrl, DownloadUrl, Path }

string GetDownloadUrl(string path, int expire = 600);
void UploadFile(string path, Stream stream, string contentType = "image/png");
string? FullUrl(string? path);           // 路径→完整URL
string? GetUrl(string? path);            // 同上
string? GetPath(string? url);            // URL→相对路径
IMinioClient Client { get; }             // 底层 MinIO 客户端
```

> **路径必须以 `/` 开头**

---

## 8. Redis 缓存 — SimApiCache

依赖 `RedisConfiguration`，Key 自动加前缀 `SimApi:Cache:`。

```csharp
void Set(string key, object value, DistributedCacheEntryOptions? options = null);
T? Get<T>(string key);
string? Get(string key);
bool HasKey(string key);
void Remove(string key);
```

---

## 9. HTTP 客户端 — SimApiHttpClient

用于调用其他带签名/AES 的 SimApi 服务。

### 属性

```csharp
virtual string Server { get; init; }
virtual string AppId   { get; init; }
virtual string AppKey  { get; init; }
virtual string SignName      { get; init; } = "sign";
virtual string TimestampName  { get; init; } = "timestamp";
virtual string NonceName     { get; init; } = "nonce";
virtual string? AppIdName    { get; init; } = "appId";
virtual string[] SignFields  { get; init; } = [];
```

### 调用方法

```csharp
T? SignQuery<T>(string url, object? body = null, Dictionary<string, string>? queries = null);
T? AesQuery<T>(string url, object body);
T? AesSignQuery<T>(string url, object body, Dictionary<string, string>? queries = null);
```

---

## 10. 任务调度 — EnableJob

Hangfire + Redis。

```csharp
options.EnableJob = true;
options.ConfigureSimApiJob(job =>
{
    job.DashboardUrl = "/jobs";           // null = 不开启
    job.DashboardAuthUser = "admin";
    job.DashboardAuthPass = "pass";
    job.Database = 1;                     // Redis DB 编号
    job.Servers = [
        new() { Queues = ["default"], WorkerNum = 5 },
        new() { Queues = ["email"],   WorkerNum = 2 }
    ];
});
```

```csharp
BackgroundJob.Enqueue(() => DoWork());
BackgroundJob.Schedule(() => DoWork(), TimeSpan.FromMinutes(5));
RecurringJob.AddOrUpdate("id", () => DoWork(), Cron.Daily);
BackgroundJob.ContinueJobWith(id, () => Step2());
```

---

## 11. MQTT 通信 — EnableSynapse

基于 MQTTnet v5，WebSocket 连接。

### 配置

```csharp
options.EnableSynapse = true;
options.ConfigureSimApiSynapse(s =>
{
    s.Websocket = "ws://mqtt:8083/mqtt";
    s.Username  = "user";
    s.Password  = "pass";
    s.SysName   = "my-system";
    s.AppName   = "order-service";
    s.AppId     = "instance-001";        // 不填自动GUID
    s.RpcTimeout = 3;                    // 秒
    s.EventLoadBalancing = false;        // $queue 负载均衡
    s.EnableConfigStore  = true;         // 分布式配置中心
});
```

### Topic 规则

| 用途 | Topic 格式 |
|------|-----------|
| 事件发布 | `{SysName}/event/{AppName}/{eventName}` |
| 事件订阅 | `{SysName}/event/{eventName}` (或 `$queue/` 前缀) |
| RPC 请求 | `{SysName}/{targetApp}/rpc/server/{method}` |
| RPC 响应 | `{SysName}/{callerApp}/rpc/client/{AppId}/{messageId}` |
| 配置 | `{SysName}/synapse-config-store/{key}` (Retain) |

### API

```csharp
// 事件
bool Event(string eventName, dynamic? param = null);

// RPC (同步)
SimApiBaseResponse<T> Rpc<T>(string appName, string method, dynamic? param = null,
    Dictionary<string, string>? headers = null, int? timeout = null);

// RPC 内报错
void RpcError(int code, string message = "");
void RpcErrorWhen(bool condition, int code, string message = "");

// 分布式配置
bool SetConfig(string key, string value);
string? GetConfig(string key);
```

### 处理器扫描（自动注册）

带 `[SynapseRpc]` / `[SynapseEvent]` 的类自动扫描注册为 Scoped 服务。

---

## 12. AES 加解密 — SimApiAesUtil

AES-256-CBC + PKCS7，密钥经 SHA256 处理。

```csharp
string cipher = SimApiAesUtil.Encrypt("明文", "任意长度密钥");
string plain  = SimApiAesUtil.Decrypt(cipher, "任意长度密钥");
```

---

## 13. 工具集 — SimApiUtil

```csharp
DateTime CstNow;                       // UTC+8
double TimestampNow;                   // 秒级 Unix
string SimApiVersion;                  // NuGet 包版本
string AppVersion;                     // 宿主应用版本

string Md5(string src, string mode = "x2");    // x2=32位, x3=48位, x4=64位
string Sha1(string src, string mode = "x2");
string Base64Encode(string str);
string Base64Decode(string base64Str);
string Base64Encode(object obj);
T? Base64Decode<T>(string base64Str);

string Json(object? obj);              // camelCase，中文不转义
T? FromJson<T>(string json);
T XmlDeserialize<T>(string xml);
JsonSerializerOptions JsonOption;      // 可复用

bool CheckCell(string cell);           // 手机号验证
bool CheckEmail(string email);

IQueryable<T> Paginate<T>(this IQueryable<T> query, int page, int count);
```

---

## 14. 数据模型 — SimApiBaseModel

```csharp
public class UserEntity : SimApiBaseModel
{
    // 自动: Id(GUID string), CreatedAt, UpdatedAt
    // 默认忽略映射: Id, CreatedAt, UpdatedAt
}

void MapData<TS>(TS source, bool mapAll = false);          // 同名同类型非null属性
void MapData<TS>(TS source, string[] mapFields);           // 指定字段
void UpdateTime();
```

---

## 15. DTO 规范

| 类型 | 命名 | 示例 |
|------|------|------|
| 请求 | `[动作]Request` | `UserEditRequest` |
| 响应 | `[动作]Response` | `TokenResponse` |
| 载体 | `[含义]Data/Item` | `GenerateData` |

### 框架内置 DTO

```csharp
class SimApiStringIdOnlyRequest { required string Id; }
class SimApiOneFieldRequest<T>    { T? Data; }
class SimApiBasePageRequest       { int Page = 1; int Count = 20; }
class SimApiBaseResponse          { int Code; string Message; }  // code=200 默认
class SimApiBaseResponse<T> : SimApiBaseResponse { T? Data; }
class PageResponse<T>             { T? List; int Page; int Count; int Total; }
```

---

## 16. SimApiOptions 完整配置

```csharp
builder.Services.AddSimApi(options =>
{
    options.RedisConfiguration = "localhost:6379";

    // 功能开关
    options.EnableSimApiAuth         = false;    // Token 认证
    options.EnableSimApiAuthGate     = false;    // Auth Center 网关鉴权
    options.EnableSimApiDoc          = false;    // Swagger 文档
    options.EnableSimApiStorage      = false;    // S3 存储
    options.EnableJob                = false;    // Hangfire
    options.EnableSynapse            = false;    // MQTT
    options.EnableSimApiHttpClient   = false;    // 外部 HTTP 调用
    options.EnableLogger             = true;     // 控制台日志
    options.EnableCors               = true;     // 全量 CORS
    options.EnableSimApiException    = true;     // 全局异常拦截
    options.EnableSimApiResponseFilter = true;   // 响应统一封装
    options.EnableForwardHeaders     = true;     // 反向代理 Header
    options.EnableLowerUrl           = true;     // URL 小写
    options.EnableVersionUrl         = true;     // /versions 接口

    // 子模块配置
    options.ConfigureSimApiDoc(doc => { ... });
    options.ConfigureSimApiStorage(s => { ... });
    options.ConfigureSimApiJob(job => { ... });
    options.ConfigureSimApiSynapse(s => { ... });
    options.ConfigureSimApiAuthCenter(gate => { ... });
    options.ConfigureSimApiHttpClient(http => { ... });
    options.ConfigureSimApiRoute(route => { ... });
    options.ConfigureSimApiException(ex => { ... });
});
```

---

## 17. GOTCHAS — 常见错误

| ❌ 错误 | ✅ 正确 |
|---------|---------|
| 存储路径 `avatars/file.jpg`（无前导 `/`） | 必须以 **`/`** 开头 |
| `s.Endpoint = "http://x:9000/"` | **不能以 `/` 结尾** |
| `synapse.PublishEvent(...)` | 方法名是 **`synapse.Event(...)`** |
| `synapse.CallRpcAsync(...)` | 方法名是 **`synapse.Rpc<T>(...)`** |
| HTTP 4xx/5xx 状态码 | 永远 **HTTP 200**，错误在 JSON code |
| `SupportedMethod` 写多种方法 | 默认仅 **POST** |
| `SimApiStorageOptions = Configuration.GetSection(...)` | 用 **`ConfigureSimApiStorage(s => {...})`** |
| 代码中 `LoginInfo.Type.Contains("admin")` | 用 `[SimApiAuth("admin")]` |
| `return ActionResult<T>` | 直接返回 Entity / void |
---

## 18. 禁止事项

| ❌ 禁止 | ✅ 正确                                                 |
|---------|------------------------------------------------------|
| HTTP 4xx/5xx 表达业务错误 | HTTP 200 + JSON `code`                               |
| `throw new Exception(msg)` | `ErrorWhen` 或 `throw new SimApiException(code, msg)` |
| 鉴权 Attribute 只放方法 | 可以放 Controller **类**上                                |
| 手动判断 `LoginInfo.Type.Contains(...)` | `[SimApiAuth("role")]`                               |
| Entity 配导航属性 / Fluent API | Convention 自动映射                                      |
| 花括号块命名空间 | 文件范围 `namespace X;`                                  |
| 传统构造函数注入 | 主构造函数                                                |
| `new List<T>()` / `new string[]{}` | `[]` 集合表达式                                           |
| `Count() > 0` | `Any()`                                              |
| `ToList()` → 数组 | 直接 `ToArray()`                                       |
| 全局 catch 吞异常 | 让异常冒泡到 SimApiExceptionMiddleware                     |
