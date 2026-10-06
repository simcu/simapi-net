using SimApi.Communications;

namespace SimApi.Interfaces;

/// <summary>
/// 自定义 Token 解析扩展点，用于让 <c>SimApiAuth.GetLogin</c> 识别本库之外签发的 Token（JWT、第三方登录态等）。
/// </summary>
/// <remarks>
/// <b>注册</b>：本库不会自动发现实现，必须由使用者自行注册。<c>AddScoped</c> / <c>AddSingleton</c> / <c>AddTransient</c>
/// 均可，因为每次调用都在独立作用域内解析。仅在 <c>EnableSimApiAuth = true</c> 时才会被执行。<br/>
/// <b>调用时机</b>：每次 <c>GetLogin</c>（即每个携带 Token 的请求）都会先执行全部 resolver，再回退到内置缓存查询；
/// 因此本机 <c>Login</c> 签发的 Token 同样会走一遍 resolver 链。<c>Logout</c> 与 <c>GetAllLogins</c> 内部也调用
/// <c>GetLogin</c>，同样会触发 resolver 链，其中 <c>GetAllLogins</c> 会按该用户的 Token 数量执行多次。<br/>
/// <b>顺序</b>：按注册顺序依次执行，返回非 <c>null</c> 即作为认证结果并立即短路，后续 resolver 与内置缓存不再参与。<br/>
/// <b>生命周期</b>：每次调用在根容器新建的子作用域内解析，因此可以安全注入 Scoped 服务（如 DbContext）。
/// 但该作用域不是当前请求的作用域，请求级事务 / UnitOfWork 的未提交改动不可见，实现应当只读。<br/>
/// <b>返回值</b>：必须是完全物化的对象。作用域在返回后立即释放，不可返回依赖延迟加载的 EF 追踪实体。<br/>
/// <b>Token 管理</b>：返回的登录态不受本库管理，<c>Login</c> / <c>Update</c> / <c>GetAllLogins</c> 不会包含它，
/// <c>Logout</c> 对它无效，吊销需由实现方自行处理。<br/>
/// <b>异常</b>：抛出的异常会直接冒泡并中断整条链（该请求认证失败）。表示“不是我的 Token”请返回 <c>null</c>，不要抛异常。<br/>
/// <b>性能</b>：这是认证热路径上的同步方法，查库等 I/O 会阻塞调用线程，建议在实现内部自行做短 TTL 缓存。
/// </remarks>
public interface ISimApiTokenResolver
{
    /// <summary>
    /// 尝试解析 Token。
    /// </summary>
    /// <param name="token">请求携带的 Token 原文。</param>
    /// <returns>解析成功返回登录信息；不属于本实现的 Token 返回 <c>null</c> 由后续 resolver 继续处理。</returns>
    public SimApiLoginItem? Run(string token);
}
