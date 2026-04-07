# SimApi AI Context

> NuGet: `Simcu.SimApi` | .NET 8/9/10 | ASP.NET Core API 基础库

---

## SETUP

```csharp
// Program.cs
builder.Services.AddSimApi(options => { ... });
var app = builder.Build();
app.UseSimApi();
app.Run();
```

---

## CORE GOTCHAS（必读，AI 容易犯的错）

| ❌ 错误 | ✅ 正确 |
|---------|---------|
| `SupportedMethod` 写多个方法 | 默认仅 `POST`，按需显式添加 |
| `WorkerNum = 50` | 默认是 `5` |
| 存储路径不加斜杠 `/avatars/file.jpg` | 路径必须以 `/` 开头 |
| `s.Endpoint = "http://minio:9000/"` | `ServeUrl`/`Endpoint` 不能以 `/` 结尾 |
| `synapse.PublishEvent(...)` | 方法名是 `synapse.Event(...)` |
| `synapse.CallRpcAsync(...)` | 方法名是 `synapse.Rpc<T>(...)` |
| HTTP 状态码 4xx/5xx 表示错误 | **所有错误均 HTTP 200**，错误在 JSON `code` 字段 |
| MQTT 用 RabbitMQ | **用 MQTTnet v5，通过 WebSocket 连接** |
| `SimApiStorageOptions = Configuration.GetSection(...)` | 用 `options.ConfigureSimApiStorage(s => {...})` |

---

## SimApiOptions（AddSimApi 配置）

```csharp
options.RedisConfiguration = "localhost:6379";  // 多模块共用
options.EnableSimApiAuth    = false;  // Token 认证
options.EnableSimApiDoc     = false;  // Swagger
options.EnableSimApiStorage = false;  // S3 存储
options.EnableJob           = false;  // Hangfire
options.EnableSynapse       = false;  // MQTT
options.EnableCoceSdk       = false;  // Coce 身份
// 以下默认 true，通常不需要改：
options.EnableCors                    = true;
options.EnableSimApiException         = true;
options.EnableSimApiResponseFilter    = true;
options.EnableForwardHeaders          = true;
options.EnableLowerUrl                = true;
options.EnableVersionUrl              = true;
options.EnableLogger                  = true;
```

---

## 响应格式

所有接口统一输出：
```json
{ "code": 200, "message": "成功", "data": { ... } }
```

HTTP 状态码**始终 200**，错误信息在 `code` 字段。

```csharp
// 无数据
return new SimApiBaseResponse();
return new SimApiBaseResponse(400, "参数错误");
return new SimApiBaseResponse(404);  // 自动映射消息

// 带数据
return new SimApiBaseResponse<T>(data);

// 分页
return new SimApiBaseResponse<PageResponse<List<T>>>(new PageResponse<List<T>>
{
    List = items, Page = 1, Count = 20, Total = 100
});
```

| 控制器返回值 | JSON 输出 |
|-------------|----------|
| `null` | `{"code":200,"message":"成功"}` |
| 普通对象 | `{"code":200,"message":"成功","data":对象}` |
| `SimApiBaseResponse` | 原样 |
| `[OriginResponse]` 方法 | 完全不封装 |

---

## SimApiBaseController

```csharp
[ApiController]
[Route("[controller]")]
public class XxxController : SimApiBaseController { }

// 错误方法（protected static）
Error(code, message)                   // 直接抛出，默认(500,"")
ErrorWhen(condition, code, message)    // condition==true 抛出，默认(400,"")
ErrorWhenTrue(condition, code, message)// 同上别名
ErrorWhenFalse(condition, code, message)// condition==false 抛出，默认(400,"")
ErrorWhenNull(obj, code, message)      // obj==null 抛出，默认(404,"请求的资源不存在")

// 当前登录信息（需 EnableSimApiAuth）
SimApiLoginItem? loginInfo = LoginInfo; // 从 HttpContext.Items["LoginInfo"] 取
```

---

## Attributes

### [SimApiAuth] — 认证
```csharp
[SimApiAuth]                     // 仅检查登录
[SimApiAuth("admin")]            // Type 包含 "admin"
[SimApiAuth("admin,manager")]    // 逗号分隔，OR 关系
```

### [SimApiDoc] — Swagger 注解
```csharp
[SimApiDoc("分组名", "接口名")]
[SimApiDoc("分组名", "接口名", "接口描述")]
[SimApiDoc(new[]{"tag1","tag2"}, "接口名")]
```

### [SynapseEvent] — MQTT 事件处理
```csharp
[SynapseEvent("order/created")]
public void OnOrderCreated(string eventName) { }

[SynapseEvent("order/+/status")]   // 支持 + 和 # 通配符
public void OnOrderStatus(string eventName, MyDto data) { }
// 参数规则：0个、1个(string eventName)、2个(string eventName, T data)
```

### [SynapseRpc] — MQTT RPC 方法
```csharp
[SynapseRpc]                    // 方法名 = "ClassName.MethodName"
[SynapseRpc("customRpcName")]   // 自定义名

// 支持 0~2 个参数，第2个固定为 Dictionary<string,string>（headers）
public UserDto GetUserInfo(GetUserRequest req) { }
public UserDto GetUserInfo(GetUserRequest req, Dictionary<string, string> headers) { }
```

### [AesBody] — AES 解密请求体
```csharp
[HttpPost]
public IActionResult Submit([AesBody(KeyProvider = typeof(MyAesKeyProvider))] MyRequest req) { }
// 客户端提交: {"data": "Base64(AES-256-CBC 加密 JSON)"}
```

### [OriginResponse] — 跳过响应封装
```csharp
[HttpGet][OriginResponse]
public string GetRaw() => "raw string";
```

### [SimApiSign] — API 签名验证
```csharp
[SimApiSign(KeyProvider = typeof(MySignProvider))]
public IActionResult SecureApi(...) { }
// 签名算法：MD5(field1=v1&...&appId=xxx&timestamp=ts&nonce=nnn&密钥)
```

---

## Auth 配置 & SimApiAuth 服务

```csharp
// Program.cs
options.EnableSimApiAuth = true;
options.RedisConfiguration = "..."; // 必须

// Token 通过 Header 传入：Token: <value>
```

```csharp
// SimApiLoginItem 结构
{ Id: string, Type: string[], Meta: Dictionary<string,string>, Extra: object? }

// DI 注入使用
public MyController(SimApiAuth auth) { }
string token = auth.Login(loginItem);                // 自动生成 GUID token
string token = auth.Login(loginItem, "custom-token");
auth.Update(loginItem, token);
SimApiLoginItem? info = auth.GetLogin(token);
auth.Logout(token);
```

自动路由（`EnableSimApiAuth` 开启后）：
- `POST /auth/check` — 检测登录状态
- `POST /auth/logout` — 退出登录
- `POST /user/info` — 获取用户信息（需登录）

---

## Swagger 配置（EnableSimApiDoc）

```csharp
options.ConfigureSimApiDoc(doc =>
{
    doc.DocumentTitle = "接口文档";
    doc.ApiGroups = [
        new("api",   "公共接口"),
        new("admin", "管理接口", "描述可选")
    ];
    doc.ApiAuth = new SimApiAuthOption { Type = ["SimApiAuth"] };
    doc.SupportedMethod = [SubmitMethod.Post]; // 默认仅 POST！
});

// 分组方式：控制器或方法加 [ApiExplorerSettings(GroupName = "admin")]
// 不加则默认归入 Id="api" 的分组
```

---

## 对象存储（EnableSimApiStorage）

```csharp
options.ConfigureSimApiStorage(s =>
{
    s.Endpoint  = "http://minio:9000";   // 不能以 / 结尾
    s.AccessKey = "admin";
    s.SecretKey = "pass";
    s.Bucket    = "my-bucket";
    s.ServeUrl  = "http://cdn.example.com/my-bucket"; // 不能以 / 结尾
});
```

```csharp
// DI 注入
public MyController(SimApiStorage storage) { }

// 路径必须以 / 开头
GetUploadUrlResponse r = storage.GetUploadUrl("/avatars/user1.jpg");
// r.UploadUrl → 前端 PUT 上传地址；r.DownloadUrl → 公开访问 URL；r.Path → 相对路径

string url = storage.GetDownloadUrl("/files/doc.pdf");           // 默认 10 分钟
string url = storage.GetDownloadUrl("/files/doc.pdf", expire: 3600);

storage.UploadFile("/path/file.jpg", stream, "image/jpeg");      // 服务端直传

string? url  = storage.FullUrl("/path/file");   // 路径转完整 URL
string? url  = storage.GetUrl("/path/file");    // 同上
string? path = storage.GetPath("http://cdn.../my-bucket/path/file"); // URL 转路径

IMinioClient mc = storage.Client;               // 暴露底层 MinIO 客户端
```

---

## 任务调度（EnableJob）

```csharp
options.ConfigureSimApiJob(job =>
{
    job.DashboardUrl      = "/jobs";     // null 则不开启 Dashboard
    job.DashboardAuthUser = "admin";
    job.DashboardAuthPass = "Admin@123!";
    job.RedisConfiguration = null;       // null 则使用全局 RedisConfiguration
    job.Database = 1;                    // Redis DB 编号，null 用默认
    job.Servers = [
        new SimApiJobServerConfig { Queues = ["default"], WorkerNum = 5 }, // 默认 WorkerNum=5
        new SimApiJobServerConfig { Queues = ["email"],   WorkerNum = 2 }
    ];
});
```

```csharp
BackgroundJob.Enqueue(() => myService.DoWork());
BackgroundJob.Schedule(() => myService.DoWork(), TimeSpan.FromMinutes(5));
RecurringJob.AddOrUpdate("job-id", () => myService.DoWork(), Cron.Daily);
var id = BackgroundJob.Enqueue(() => Step1());
BackgroundJob.ContinueJobWith(id, () => Step2());
```

---

## MQTT 通信（EnableSynapse）

```csharp
options.ConfigureSimApiSynapse(s =>
{
    s.Websocket = "ws://mqtt:8083/mqtt"; // WebSocket 连接
    s.Username  = "user";
    s.Password  = "pass";
    s.SysName   = "my-system";           // Topic 命名空间前缀
    s.AppName   = "order-service";       // 服务名
    s.AppId     = "instance-001";        // 实例ID，不填自动 GUID
    s.RpcTimeout = 3;                    // RPC 超时秒数
    s.EventLoadBalancing  = false;       // $queue 订阅负载均衡
    s.EnableConfigStore   = true;        // 分布式配置中心
    s.DisableEventClient  = false;
    s.DisableRpcClient    = false;
});
```

Topic 规则：
```
事件发布：   {SysName}/event/{AppName}/{eventName}
事件订阅：   {SysName}/event/{eventName}（或 $queue/... 启用负载均衡）
RPC 请求：   {SysName}/{targetApp}/rpc/server/{method}
RPC 响应：   {SysName}/{callerApp}/rpc/client/{AppId}/{messageId}
配置存储：   {SysName}/synapse-config-store/{key}（Retain）
```

```csharp
// DI 注入
public MyService(Synapse synapse) { }

synapse.Event("order/created", new { OrderId = 1 });  // 发布事件

// RPC 调用（同步，返回 SimApiBaseResponse<T>）
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

处理器类（含 `[SynapseRpc]`/`[SynapseEvent]` 的类无需手动注册，自动扫描为 Scoped）：
```csharp
public class OrderEventHandler
{
    [SynapseEvent("order/+/status")]
    public void OnOrderStatus(string eventName, OrderStatusDto data) { }
}

public class UserRpcService
{
    [SynapseRpc]  // 注册为 "UserRpcService.GetUserInfo"
    public UserDto GetUserInfo(GetUserRequest req) { return ...; }

    [SynapseRpc("customName")]
    public ResultDto DoSomething(RequestDto req, Dictionary<string, string> headers) { }
}
```

---

## API 签名验证（[SimApiSign]）

```csharp
// 1. 实现密钥提供者
public class MySignProvider : SimApiSignProviderBase
{
    public override string? AppIdName   { get; set; } = "appId";
    public override string TimestampName { get; set; } = "timestamp";
    public override string NonceName    { get; set; } = "nonce";
    public override string SignName     { get; set; } = "sign";
    public override int    QueryExpires { get; set; } = 5;
    public override bool   DuplicateRequestProtection { get; set; } = true;
    public override string[] SignFields { get; set; } = ["userId"]; // 额外签名字段

    public override string? GetKey(string? appId)
    {
        // 根据 appId 返回密钥
        return db.Apps.Find(appId)?.SecretKey;
    }
}
services.AddScoped<MySignProvider>(); // 注册

// 2. 使用
[SimApiSign(KeyProvider = typeof(MySignProvider))]
public IActionResult SecureApi(...) { }
```

---

## AES 加密传输（[AesBody]）

算法：**AES-256-CBC + PKCS7**，IV 随机生成附在密文前，整体 Base64 编码。

```csharp
// 1. 实现密钥提供者
public class MyAesKeyProvider : AesBodyProviderBase
{
    public override string? AppIdName { get; set; } = "appId"; // 从 Query/Header 取
    public override string? GetKey(string? appId) => db.Apps.Find(appId)?.SecretKey;
}
services.AddScoped<MyAesKeyProvider>();

// 2. 使用（客户端提交 {"data": "Base64密文"}）
[HttpPost]
public IActionResult Submit([AesBody(KeyProvider = typeof(MyAesKeyProvider))] MyRequest req) { }

// 工具类（静态，无需注入）
string cipher = SimApiAesUtil.Encrypt("明文", "任意长度密钥"); // SHA256 处理为 32 字节
string plain  = SimApiAesUtil.Decrypt(cipher, "任意长度密钥");
```

---

## Redis 缓存（SimApiCache）

> 依赖 `RedisConfiguration`，key 自动加前缀 `SimApi:Cache:`

```csharp
public MyService(SimApiCache cache) { }

cache.Set("key", value);                                              // 永不过期
cache.Set("key", value, new DistributedCacheEntryOptions {
    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
});
string? raw  = cache.Get("key");                                      // 原始字符串
int?    val  = cache.Get<int>("key");                                 // 反序列化
```

---

## HTTP 客户端（SimApiHttpClient）

```csharp
var client = new SimApiHttpClient(appId: "myapp", appKey: "secret")
{
    Server        = "https://api.example.com",
    AppIdName     = "appId",
    TimestampName = "timestamp",
    NonceName     = "nonce",
    SignName      = "sign",
    SignFields    = ["field1"]
};

var r = client.SignQuery<T>("/api/user", body, queries);    // 仅签名
var r = client.AesQuery<T>("/api/user", body);             // 仅 AES 加密
var r = client.AesSignQuery<T>("/api/user", body, queries);// AES + 签名
```

---

## 工具类（SimApiUtil，全部静态）

```csharp
DateTime cst       = SimApiUtil.CstNow;          // UTC+8 当前时间
double   ts        = SimApiUtil.TimestampNow;     // 秒级 Unix 时间戳
string   simVer    = SimApiUtil.SimApiVersion;    // SimApi 包版本
string   appVer    = SimApiUtil.AppVersion;       // 宿主应用版本

string md5  = SimApiUtil.Md5("src");              // 32位 MD5
string md5  = SimApiUtil.Md5("src", "x3");        // 48位
string sha1 = SimApiUtil.Sha1("src");

string json = SimApiUtil.Json(obj);               // camelCase，中文不转义
T obj       = SimApiUtil.XmlDeserialize<T>(xml);
JsonSerializerOptions opts = SimApiUtil.JsonOption;

bool ok = SimApiUtil.CheckCell("13800138000");    // 手机号验证

// IQueryable 扩展
var paged = dbContext.Users.AsQueryable().Paginate(page: 1, count: 20);
```

---

## 数据模型基类（SimApiBaseModel）

```csharp
public class UserEntity : SimApiBaseModel
{
    public string Name { get; set; }
    // 自动字段：Id(GUID string)、CreatedAt、UpdatedAt
}

entity.MapData(dto);                      // 跳过 Id/CreatedAt/UpdatedAt
entity.MapData(dto, mapAll: true);        // 映射所有字段
entity.MapData(dto, new[]{"Name","Email"}); // 只映射指定字段
entity.UpdateTime();                      // 手动更新 UpdatedAt
// 注意：只映射同名+同类型+源值不为null 的属性
```

---

## Coce 统一身份（EnableCoceSdk）

> 同时需要 `EnableSimApiAuth = true`

```csharp
options.ConfigureCoceSdk(coce =>
{
    coce.ApiEndpoint  = "https://api.coce.cc";   // 默认
    coce.AuthEndpoint = "https://home.coce.cc";  // 默认
    coce.AppId  = "your-app-id";
    coce.AppKey = "your-app-key";
});
```

```csharp
// DI 注入
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
coce.ProxyQuery<T>(uri, token, json)
coce.ProxyQueue<T>(uri, token, data)
```

自动路由：
- `POST /auth/login` — Coce 一键登录（前端传 `{"data":"lv1Token"}`）
- `POST /user/groups` — 获取用户群组（需登录）
- `GET /auth/config` — 获取 AppId 和授权 URL

自定义登录逻辑：
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
services.AddScoped<ICoceLoginProcessor, MyLoginProcessor>();
```

---

## 内置路由汇总

| 路由 | 方法 | 条件 |
|------|------|------|
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

## 异常处理流程

```
请求进入
 └─ SimApiExceptionMiddleware（捕获所有异常 → HTTP 200 + code 字段）
     └─ SimApiAuthMiddleware（解析 Token）
         └─ [SimApiSign] Filter
             └─ [SimApiAuth] Filter
                 └─ OnActionExecuting（模型验证 → code 400）
                     └─ Action 执行
                         └─ SimApiResponseFilter（封装响应）
```
