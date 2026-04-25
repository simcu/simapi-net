# C# .NET Web API 编码规范（SimApi 框架版）

> **适用范围**：所有基于 `Simcu.SimApi` 框架的 .NET Web API 项目，适用于任何 AI 辅助编程工具（Claude、ChatGPT、GitHub Copilot、Cursor 等）。
>
> **使用方式**：将本文档作为上下文提供给 AI，或在对话开头粘贴。
>
> **核心原则**：优先遵循已有代码的风格；本文档描述的是偏好，不是必须逐条套用的模板。

---

## 一、技术栈

```
NuGet: Simcu.SimApi
.NET 8 / 9 / 10
C# 12 / 14
Nullable: enable
ImplicitUsings: enable
```

---

## 二、SimApi 核心概念（必读）

### 2.1 响应格式

**所有接口统一输出 JSON，HTTP 状态码始终 200，错误信息在 `code` 字段**：

```json
{ "code": 200, "message": "成功", "data": { ... } }
```

不要用 HTTP 4xx/5xx 表达业务错误。

### 2.2 常见错误（AI 容易犯的错）

| ❌ 错误 | ✅ 正确 |
|---------|---------|
| `SupportedMethod` 写多个方法 | 默认仅 `POST`，按需显式添加 |
| `WorkerNum = 50` | 默认是 `5` |
| 存储路径不加斜杠 `/avatars/file.jpg` | 路径必须以 `/` 开头 |
| `s.Endpoint = "http://minio:9000/"` | `ServeUrl`/`Endpoint` **不能以 `/` 结尾** |
| `synapse.PublishEvent(...)` | 方法名是 `synapse.Event(...)` |
| `synapse.CallRpcAsync(...)` | 方法名是 `synapse.Rpc<T>(...)` |
| HTTP 状态码 4xx/5xx 表示错误 | **所有错误均 HTTP 200**，错误在 JSON `code` 字段 |
| `SimApiStorageOptions = Configuration.GetSection(...)` | 用 `options.ConfigureSimApiStorage(s => {...})` |
| `[HttpGet]` / `[HttpPut]` / `[HttpDelete]` | **默认仅 POST**，其他方法需在 `SupportedMethod` 显式添加 |

### 2.3 异常处理流程

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

---

## 三、C# 语言特性偏好

### 3.1 命名空间

使用**文件范围命名空间**，不用花括号块：

```csharp
// ✅
namespace MyApp.Controllers;

// ❌
namespace MyApp.Controllers { }
```

### 3.2 主构造函数（依赖注入）

使用**主构造函数**注入依赖，不写传统构造函数：

```csharp
// ✅
public class OrderController(DataContext db) : SimApiBaseController
{
}

// ❌
public class OrderController : SimApiBaseController
{
    private readonly DataContext _db;
    public OrderController(DataContext db) { _db = db; }
}
```

### 3.3 集合表达式

优先 `[]`，避免冗余的 `new`：

```csharp
// ✅
string[] tags = [];
string[] roles = ["admin", "manager"];

// ❌
var tags = new string[] { };
var roles = new string[] { "admin", "manager" };
```

### 3.4 字符串

优先字符串插值，不用 `string.Format`：

```csharp
// ✅
var msg = $"用户 {user.Name} 不存在";

// ❌
var msg = string.Format("用户 {0} 不存在", user.Name);
```

### 3.5 Null 处理

```csharp
var key = app?.Key;                         // 安全访问
var name = user?.Name ?? "匿名";            // 空合并
config ??= new Dictionary<string, string>(); // 空合并赋值
```

---

## 四、命名规范

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

## 五、项目目录结构

推荐**极简扁平化**，无 Service 层、无 Repository 层：

```
项目名/
├── Controllers/          # 控制器（含业务逻辑）
│   └── Dtos/             # 请求/响应 DTO
├── Models/               # EF Core 实体 + DataContext
├── Helpers/              # 工具类 / 框架扩展点
├── Migrations/           # EF Core 迁移（自动生成，勿手改）
└── Program.cs            # 入口 + DI + 中间件（无 Startup.cs）
```

业务逻辑直接在 Controller 中通过 `db`（EF Core DbContext）操作数据库。可复用的横切逻辑抽取到 `Helpers/`。

> 如果项目较复杂，也可以选择标准分层（Controllers → Services → Models），但需在项目内保持一致，不要混用。

---

## 六、Program.cs 规范

使用顶级语句（无 `Main` 方法、无 `Startup` 类）：

```csharp
var builder = WebApplication.CreateBuilder(args);

// 1. SimApi 框架配置
builder.Services.AddSimApi(options =>
{
    options.RedisConfiguration = builder.Configuration.GetConnectionString("Redis");
    options.EnableSimApiAuth = true;     // 按需开启
    options.EnableSimApiDoc = true;      // 按需开启
    options.EnableSimApiStorage = false; // 按需开启
    options.EnableJob = false;           // 按需开启
    options.EnableSynapse = false;       // 按需开启

    // Swagger 配置
    options.ConfigureSimApiDoc(doc =>
    {
        doc.DocumentTitle = "接口文档";
        doc.ApiGroups = [
            new("api", "公共接口"),
            new("admin", "管理接口")
        ];
        doc.SupportedMethod = [SubmitMethod.Post]; // 默认仅 POST
    });

    // 存储配置（按需）
    // options.ConfigureSimApiStorage(s => { ... });
});

// 2. 数据库
builder.Services.AddDbContext<DataContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// 3. 框架扩展点（用接口注册）
builder.Services.AddScoped<AesBodyProviderBase, AesBodyProvider>();
builder.Services.AddScoped<SimApiSignProviderBase, SimApiSignProvider>();

// 4. 项目自定义服务
builder.Services.AddScoped<ResPermission>();
builder.Services.AddSingleton<JsonSchemaHelper>();

var app = builder.Build();

// 5. 启动时自动迁移（同步写法）
app.Services.CreateScope().ServiceProvider
    .GetRequiredService<DataContext>().Database.Migrate();

// 6. 框架中间件
app.UseSimApi();
app.Run();
```

### SimApiOptions 常用配置

```csharp
options.RedisConfiguration = "localhost:6379";  // Redis（多模块共用）
options.EnableSimApiAuth    = false;  // Token 认证
options.EnableSimApiDoc     = false;  // Swagger
options.EnableSimApiStorage = false;  // S3 存储
options.EnableJob           = false;  // Hangfire 任务调度
options.EnableSynapse       = false;  // MQTT 通信
options.EnableCoceSdk       = false;  // Coce 统一身份
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

## 七、控制器规范

### 7.1 基类与依赖注入

所有 Controller 继承 `SimApiBaseController`，依赖通过**主构造函数**注入：

```csharp
[SimApiAuth]
public class DeviceController(DataContext db) : SimApiBaseController
{
}
```

### 7.2 路由规则

**默认全部使用 POST**（除非在 `SupportedMethod` 中显式添加其他方法）：

```csharp
// 方法上写完整路径
[HttpPost("/device/list")]
[HttpPost("/application/refresh-key")]

// 类上写前缀，方法上写相对路径
[Route("/platform")]
public class PlatformController(DataContext db) : SimApiBaseController
{
    [HttpPost("device/detail")]    // 最终路由：/platform/device/detail
    [HttpPost("bot/generate")]     // 最终路由：/platform/bot/generate
}
```

路由路径全小写，多词用连字符 `-` 分隔。

### 7.3 鉴权 Attribute

| Attribute | 用途 |
|-----------|------|
| `[SimApiAuth]` | 要求登录用户 |
| `[SimApiAuth("admin")]` | 要求 admin 角色 |
| `[SimApiAuth("admin,manager")]` | 逗号分隔，OR 关系 |
| `[SimApiSign(KeyProvider = typeof(XxxProvider))]` | API 签名验证 |

> 鉴权 Attribute 写在 **Controller 类** 上，不写在方法上。

### 7.4 接口分组与文档

```csharp
[ApiExplorerSettings(GroupName = "platform")]
[SimApiDoc("设备", "获取设备详情")]
[HttpPost("device/detail")]
public Device DeviceDetail(...)
```

### 7.5 方法返回值

| 场景 | 返回类型 |
|------|----------|
| 写操作（新增/修改/删除） | `void`（框架自动返回 `{"code":200}`） |
| 单条查询 | 直接返回 Entity（如 `Account`、`Device`） |
| 列表查询 | `Entity[]`（数组，不用 `List<T>`） |
| 分页查询 | `PageResponse<Entity[]>` |
| 有状态响应 | `SimApiBaseResponse` |
| 复杂组合响应 | 对应 DTO |

> **不使用** `ActionResult<T>` 或 `IActionResult`（除非使用 `[AesBody]` 等框架 Attribute）。

### 7.6 方法参数

```csharp
// 普通请求体
[FromBody] DeviceDto.SerialAddRequest request

// AES 加密请求体
[AesBody(KeyProvider = typeof(AesBodyProvider))] BotDto.BotChatRequest request

// 查询字符串（直接写，不加 Attribute）
string appId
```

### 7.7 错误处理

使用基类的 `ErrorWhen` 系列方法，**不手动 throw、不返回错误码**：(全局扩展,任何地方都可以这样抛出异常)

```csharp
ErrorWhenNull(entity);                           // null 则报错（默认 404）
ErrorWhenNull(entity, 404, "用户不存在");          // 自定义状态码和消息
ErrorWhen(condition, 400, "已经共享过了");          // condition 为 true 则报错
ErrorWhenFalse(condition, 403, "你无权操作");      // condition 为 false 则报错
```

在 Controller 外部（如 Helper 中）需要抛出异常时，使用 `SimApiException`：

```csharp
throw new SimApiException(404, "App不存在");
throw new SimApiException(403, "没有权限修改对应资源");
```

### 7.8 当前登录信息

```csharp
// 通过基类属性获取（需 EnableSimApiAuth）
var userId = LoginInfo?.Id;
var userRole = LoginInfo?.Type;  // string[]

// 权限检查示例
ErrorWhen(!LoginInfo.Type.Contains("admin"), 403, "需要管理员权限");
```

### 7.9 私有辅助方法

Controller 内部可复用的逻辑提取为 `private` 方法，不独立成 Service：

```csharp
private void CheckAppId(string appId)
{
    ErrorWhen(!db.Applications.Any(x => x.AccountId == LoginInfo.Id && x.Id == appId), 403, "无权操作");
}
```

---

## 八、DTO 规范

### 8.1 组织方式

DTO 文件放在 `Controllers/Dtos/`，按业务域命名 `XxxDto.cs`。使用嵌套容器类：

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

### 8.2 命名规则

| 类型 | 格式 | 示例 |
|------|------|------|
| 请求 DTO | `[动作]Request` | `UserEditRequest`、`DeviceSerialAddRequest` |
| 响应 DTO | `[动作]Response` | `GenerateResponse`、`TokenResponse` |
| 数据载体 | `[含义]Data` | `GenerateData`、`AgentItem` |

引用时用全限定名：`AdminDto.UserEditRequest`、`PlatformDto.GenerateResponse`。

### 8.3 属性规则

```csharp
public class DeviceSerialAddRequest
{
    public required string Verify { get; set; }       // 必填
    public required string AppId { get; set; }
    public required string Name { get; set; }
    [Range(1, 10000)] public required int Num { get; set; }  // 范围校验
    public string? Remark { get; set; }               // 可选
    public int Status { get; set; } = 1;              // 有默认值
}
```

### 8.4 框架内置通用 DTO（优先复用）

| 框架 DTO | 用途 |
|----------|------|
| `SimApiStringIdOnlyRequest` | 只有 `Id` 字段的请求 |
| `SimApiOneFieldRequest<T>` | 只有一个 `Data` 字段的请求 |
| `SimApiBasePageRequest` | 分页请求基类（含 `Page`、`Count`） |
| `SimApiBaseResponse` | 通用状态响应（可传 code + message） |
| `SimApiBaseResponse<T>` | 带数据的响应 |
| `PageResponse<T>` | 分页响应（含 `Total`、`Page`、`Count`、`List`） |

---

## 九、Entity 规范

### 9.1 基类

所有实体继承 `SimApiBaseModel`（自动提供 `Id`、`CreatedAt`、`UpdatedAt`）：

```csharp
using SimApi.Models;

namespace MyApp.Models;

public class Account : SimApiBaseModel
{
    public required string Name { get; set; }
    public required string Username { get; set; }
    public string? Password { get; set; }
    public required string Role { get; set; } = "user";
    public int Status { get; set; } = 1;
}
```

### 9.2 对象映射（MapData）

`SimApiBaseModel` 提供 `MapData` 方法，用于 DTO ↔ Entity 映射：

```csharp
// 跳过 Id/CreatedAt/UpdatedAt，映射同名同类型且源值不为 null 的属性
entity.MapData(dto);

// 映射所有字段
entity.MapData(dto, mapAll: true);

// 只映射指定字段
entity.MapData(dto, new[] { "Name", "Email" });

// 手动更新 UpdatedAt
entity.UpdateTime();
```

### 9.3 属性规则

- 必填字段用 `required`，可选字段用 `?`，有默认值的直接赋值
- 外键命名：`[关联实体]Id`，如 `AccountId`、`AppId`、`ServiceId`
- **不配置导航属性**，不配置 EF Fluent API，依赖 Convention 自动映射

### 9.4 DataContext

只定义 DbSet，不做任何 Fluent API 配置：

```csharp
public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
{
    public required DbSet<Account> Accounts { get; set; }
    public required DbSet<Application> Applications { get; set; }
    public required DbSet<Device> Devices { get; set; }
}
```

---

## 十、EF Core 查询风格

```csharp
// 列表查询（排序 + ToArray）
db.Accounts.OrderBy(x => x.CreatedAt).ToArray();

// 动态条件查询（AsQueryable 后追加 Where）
var query = db.Devices
    .Where(x => x.ApplicationId == appId)
    .OrderBy(x => x.CreatedAt)
    .AsQueryable();
if (!string.IsNullOrEmpty(request.Serial))
    query = query.Where(x => x.Serial == request.Serial);

// 分页查询（框架扩展方法 Paginate）
var list = query.Paginate(request.Page, request.Count).ToArray();
var total = query.Count();
return new PageResponse<Device[]> { List = list, Total = total, Page = request.Page, Count = request.Count };

// 单条查询
db.Accounts.Find(id);                                        // 主键用 Find
db.Accounts.FirstOrDefault(x => x.Username == username);    // 其他条件用 FirstOrDefault

// 写操作
db.Add(entity);       // 新增
db.Update(entity);    // 修改
db.Remove(entity);    // 删除
db.SaveChanges();     // 统一在所有操作完成后调用一次

// 存在性判断（不用 Count）
db.AppServices.Any(x => x.ServiceId == request.Id)
```

---

## 十一、框架扩展点（Helpers/）

### 11.1 AES 加密请求体

实现 `AesBodyProviderBase`，提供解密密钥：

```csharp
public class AesBodyProvider(DataContext db) : AesBodyProviderBase
{
    public override string? AppIdName { get; set; } = "appId";

    public override string? GetKey(string? appId)
    {
        var application = db.Applications.Find(appId);
        return application?.Key;
    }
}
```

客户端提交格式：`{"data": "Base64(AES-256-CBC 加密 JSON)"}`

静态工具类（无需注入）：
```csharp
string cipher = SimApiAesUtil.Encrypt("明文", "任意长度密钥");
string plain  = SimApiAesUtil.Decrypt(cipher, "任意长度密钥");
```

### 11.2 API 签名验证

实现 `SimApiSignProviderBase`：

```csharp
public class SimApiSignProvider(DataContext db) : SimApiSignProviderBase
{
    public override string? AppIdName   { get; set; } = "appId";
    public override string TimestampName { get; set; } = "timestamp";
    public override string NonceName    { get; set; } = "nonce";
    public override string SignName     { get; set; } = "sign";
    public override int    QueryExpires { get; set; } = 5;
    public override bool   DuplicateRequestProtection { get; set; } = true;
    public override string[] SignFields { get; set; } = ["userId"];

    public override string? GetKey(string? appId)
    {
        return db.Applications.Find(appId)?.SecretKey;
    }
}
```

> 扩展点用**接口注册**：`builder.Services.AddScoped<AesBodyProviderBase, AesBodyProvider>()`

### 11.3 Helper 使用原则

Helper 仅用于以下场景，**不承担 CRUD 业务逻辑**：

1. **框架扩展点**：继承 `XxxProviderBase` 并 override 方法
2. **横切关注点**：权限校验、短信发送
3. **纯工具静态类**：无状态工具方法
4. **有状态单例/Scoped 服务**：如 JSON Schema 验证

---

## 十二、框架功能模块

### 12.1 认证（SimApiAuth）

```csharp
// Program.cs
options.EnableSimApiAuth = true;
options.RedisConfiguration = "..."; // 必须

// Token 通过 Header 传入：Token: <value>

// DI 注入 SimApiAuth 服务
public MyController(SimApiAuth auth) { }

// 登录
string token = auth.Login(loginItem);                // 自动生成 GUID token
string token = auth.Login(loginItem, "custom-token");
auth.Update(loginItem, token);

// 查询/退出
SimApiLoginItem? info = auth.GetLogin(token);
auth.Logout(token);

// SimApiLoginItem 结构
// { Id: string, Type: string[], Meta: Dictionary<string,string>, Extra: object? }
```

自动路由（开启 `EnableSimApiAuth` 后可用）：
- `POST /auth/check` — 检测登录状态
- `POST /auth/logout` — 退出登录
- `POST /user/info` — 获取用户信息（需登录）

### 12.2 对象存储（SimApiStorage）

```csharp
// Program.cs
options.EnableSimApiStorage = true;
options.ConfigureSimApiStorage(s =>
{
    s.Endpoint  = "http://minio:9000";     // 不能以 / 结尾
    s.AccessKey = "admin";
    s.SecretKey = "pass";
    s.Bucket    = "my-bucket";
    s.ServeUrl  = "http://cdn.example.com/my-bucket"; // 不能以 / 结尾
});

// DI 注入
public MyController(SimApiStorage storage) { }

// 路径必须以 / 开头
storage.GetUploadUrl("/avatars/user1.jpg");           // 返回上传地址和下载地址
storage.GetDownloadUrl("/files/doc.pdf");             // 默认 10 分钟过期
storage.GetDownloadUrl("/files/doc.pdf", expire: 3600);
storage.UploadFile("/path/file.jpg", stream, "image/jpeg");  // 服务端直传
storage.FullUrl("/path/file");                        // 路径转完整 URL
storage.GetPath("http://cdn.../my-bucket/path/file"); // URL 转路径
```

### 12.3 Redis 缓存（SimApiCache）

```csharp
public MyService(SimApiCache cache) { }

cache.Set("key", value);                                              // 永不过期
cache.Set("key", value, new DistributedCacheEntryOptions {
    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
});
string? raw = cache.Get("key");                                       // 原始字符串
int? val    = cache.Get<int>("key");                                  // 反序列化
// key 自动加前缀 SimApi:Cache:
```

### 12.4 任务调度（Hangfire）

```csharp
// Program.cs
options.EnableJob = true;
options.ConfigureSimApiJob(job =>
{
    job.DashboardUrl      = "/jobs";
    job.DashboardAuthUser = "admin";
    job.DashboardAuthPass = "Admin@123!";
    job.Servers = [
        new SimApiJobServerConfig { Queues = ["default"], WorkerNum = 5 },
        new SimApiJobServerConfig { Queues = ["email"],   WorkerNum = 2 }
    ];
});

// 使用
BackgroundJob.Enqueue(() => myService.DoWork());
BackgroundJob.Schedule(() => myService.DoWork(), TimeSpan.FromMinutes(5));
RecurringJob.AddOrUpdate("job-id", () => myService.DoWork(), Cron.Daily);
var id = BackgroundJob.Enqueue(() => Step1());
BackgroundJob.ContinueJobWith(id, () => Step2());
```

### 12.5 MQTT 通信（Synapse）

```csharp
// Program.cs
options.EnableSynapse = true;
options.ConfigureSimApiSynapse(s =>
{
    s.Websocket = "ws://mqtt:8083/mqtt";  // WebSocket 连接（不是 RabbitMQ）
    s.Username  = "user";
    s.Password  = "pass";
    s.SysName   = "my-system";
    s.AppName   = "order-service";
    s.AppId     = "instance-001";
    s.RpcTimeout = 3;
});

// DI 注入
public MyService(Synapse synapse) { }

// 发布事件
synapse.Event("order/created", new { OrderId = 1 });

// RPC 调用（同步，返回 SimApiBaseResponse<T>）
var res = synapse.Rpc<UserDto>("user-service", "GetUserInfo", new { Id = 1 });

// RPC 方法内部抛错
synapse.RpcError(400, "参数错误");
synapse.RpcErrorWhen(id <= 0, 400, "ID 无效");

// 事件处理器（自动扫描注册）
public class OrderEventHandler
{
    [SynapseEvent("order/+/status")]   // 支持 + 和 # 通配符
    public void OnOrderStatus(string eventName, OrderStatusDto data) { }
}

// RPC 服务（自动扫描注册）
public class UserRpcService
{
    [SynapseRpc]  // 注册为 "UserRpcService.GetUserInfo"
    public UserDto GetUserInfo(GetUserRequest req) { return ...; }

    [SynapseRpc("customName")]
    public ResultDto DoSomething(RequestDto req, Dictionary<string, string> headers) { }
}
```

### 12.6 HTTP 客户端（SimApiHttpClient）

```csharp
var client = new SimApiHttpClient(appId: "myapp", appKey: "secret")
{
    Server = "https://api.example.com",
};

var r = client.SignQuery<T>("/api/user", body, queries);     // 仅签名
var r = client.AesQuery<T>("/api/user", body);                // 仅 AES 加密
var r = client.AesSignQuery<T>("/api/user", body, queries);   // AES + 签名
```

### 12.7 工具类（SimApiUtil，全部静态）

```csharp
DateTime cst    = SimApiUtil.CstNow;          // UTC+8 当前时间
double   ts     = SimApiUtil.TimestampNow;     // 秒级 Unix 时间戳
string   md5    = SimApiUtil.Md5("src");       // 32位 MD5
string   json   = SimApiUtil.Json(obj);        // camelCase，中文不转义
bool ok         = SimApiUtil.CheckCell("13800138000"); // 手机号验证
```

---

## 十三、配置文件规范

`appsettings.json` 只保留框架默认值：

```json
{
  "Logging": { "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" } },
  "AllowedHosts": "*"
}
```

`appsettings.Development.json` 存放开发环境实际配置（不提交到 Git）：

```json
{
  "ConnectionStrings": {
    "Default": "Host=...;Database=...;Username=...;Password=...",
    "Redis": "host:port,defaultDatabase=N"
  },
  "Sms": { "Account": "...", "Password": "..." }
}
```

配置读取：

```csharp
builder.Configuration.GetConnectionString("Default")
config["Gateway:Key"]
config.GetSection("Sms").GetSection("Templates")["verify"]
```

---

## 十四、注释规范

- **公有 API / 方法**：写 XML 文档注释
- **私有方法**：逻辑简单可不写；复杂逻辑写行内注释说明**为什么**
- **不要写废话注释**

```csharp
/// <summary>
/// 根据邮箱查询用户，不存在返回 null。
/// </summary>
public Account? FindByEmail(string email)
    => db.Accounts.FirstOrDefault(x => x.Email == email);

// ❌ 废话注释
// 查询用户
var user = db.Accounts.Find(id);

// ✅ 有意义的注释
// EF Core 的 Find 会优先命中一级缓存
var user = db.Accounts.Find(id);
```

---

## 十五、禁止事项

以下模式在使用 SimApi 框架时**明确禁止**：

| ❌ 禁止 | ✅ 正确做法 |
|---------|------------|
| 使用 HTTP 4xx/5xx 表达业务错误 | HTTP 200 + JSON `code` 字段 |
| `throw new Exception(message)` | `ErrorWhen` 系列或 `SimApiException` |
| 新建 Service / Repository 层（除非项目明确需要） | Controller 直接操作 DbContext |
| 使用 `ActionResult<T>` / `IActionResult` | 直接返回 Entity / `void` / `SimApiBaseResponse` |
| 在 Controller 方法上加鉴权 Attribute | 加在 Controller 类上 |
| 在 Entity 中配置导航属性或 EF Fluent API | 依赖 Convention 自动映射 |
| 在 DbContext 中写 `OnModelCreating`（除非需要全局过滤等） | 只定义 DbSet |
| 花括号块命名空间 | 文件范围命名空间 |
| 传统构造函数注入 | 主构造函数 |
| `new List<T>()` 初始化空集合 | `[]` 集合表达式 |
| `Count() > 0` 判断存在 | `Any()` |
| `ToList()` 再转数组 | 直接 `ToArray()` |
| `string.IsNullOrEmpty` 判断必填入参 | `required` 修饰符 + 模型验证 |
| 全局 `catch (Exception e) { log; return null; }` | 让异常冒泡，由 SimApiExceptionMiddleware 处理 |
