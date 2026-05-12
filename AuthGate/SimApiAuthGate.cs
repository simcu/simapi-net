using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using SimApi.Communications;
using static SimApi.Helpers.SimApiError;

namespace SimApi.AuthGate;

public class SimApiAuthGate(SimApiAuthGateClient simapi)
{
    #region AuthGate公开接口

    /// <summary>
    /// 委托AuthCenter进行应用签名验证
    /// </summary>
    /// <param name="appId"></param>
    /// <param name="timestamp"></param>
    /// <param name="nonce"></param>
    /// <param name="sign"></param>
    public void VerifySign(string appId, string timestamp, string nonce, string sign)
    {
        var http = new HttpClient();
        var url = $"{simapi.Server}/api/auth/sign/verify?appId={appId}&timestamp={timestamp}&nonce={nonce}&sign={sign}";
        var result = http.PostAsJsonAsync(url, new { }).Result;
        var resp = result.Content.ReadFromJsonAsync<SimApiBaseResponse>().Result;
        ErrorWhenNull(resp, 400, "签名验证失败");
        ErrorWhenFalse(resp.Code == 200, 400, "签名验证失败");
    }

    /// <summary>
    /// 根据关键字,搜索用户Profile,参数支持精准ID,用户手机号,用户邮箱,Profile名称模糊搜索
    /// </summary>
    /// <param name="keyword"></param>
    /// <param name="skip"></param>
    /// <param name="take"></param>
    /// <returns></returns>
    public SimApiAuthGateDto.AppAndProfileItem[]? ProfileSearch(string keyword, int skip = 0, int take = 20)
    {
        return simapi.SignQuery<SimApiAuthGateDto.AppAndProfileItem[]>("/api/auth/profile/search",
            new { keyword, skip, take });
    }

    /// <summary>
    /// 通过id,可以批量获取用户的基本信息
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    public SimApiAuthGateDto.AppAndProfileItem[]? ProfileList(string[] ids)
    {
        return simapi.SignQuery<SimApiAuthGateDto.AppAndProfileItem[]>("/api/auth/profile/list", new { ids });
    }

    #endregion

    #region AuthGate内部应用专用 - 注意: 只有内部应用可以调用

    /// <summary>
    /// 获取是否为App的拥有者
    /// </summary>
    /// <param name="profileId"></param>
    /// <param name="applicationId"></param>
    /// <returns></returns>
    public bool CheckIsAppOwner(string profileId, string applicationId)
    {
        return simapi.SignQuery<bool>("/api/auth/internal/apps/check-owner", new
        {
            ProfileId = profileId,
            AppId = applicationId,
        });
    }

    /// <summary>
    /// 获取应用列表,根据用户profileId 和 提供的appIds
    /// </summary>
    /// <param name="profileId"></param>
    /// <param name="appIds"></param>
    /// <returns></returns>
    public SimApiAuthGateDto.AppAndProfileItem[]? GetAppList(string profileId, IEnumerable<string> appIds)
    {
        return simapi.SignQuery<SimApiAuthGateDto.AppAndProfileItem[]>("/api/auth/internal/apps/related", new
        {
            ProfileId = profileId,
            AllowedAppIds = appIds,
        });
    }

    #endregion

    #region 系统登录

    /// <summary>
    /// 获取登录授权CODE
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="data"></param>
    /// <param name="backUrl"></param>
    /// <returns></returns>
    public SimApiAuthGateDto.GetCodeResponse GetLoginCode(string? scene = null, Dictionary<string, object>? data = null,
        string? backUrl = null)
    {
        var code = simapi.SignQuery<string>("/api/auth/login/code",
            new { scene, data, backUrl });
        return new SimApiAuthGateDto.GetCodeResponse()
        {
            Code = code!,
            Server = simapi.Server,
            FullUrl = $"{simapi.Server}/auth?code={code}"
        };
    }

    /// <summary>
    /// 使用code获取登录信息
    /// </summary>
    /// <param name="code"></param>
    /// <param name="scene"></param>
    /// <returns></returns>
    public SimApiAuthGateDto.LoginInfoResponse GetLoginInfo(string code, string? scene = null)
    {
        var resp = simapi.SignQuery<SimApiAuthGateDto.LoginInfoResponse>("/api/auth/login/get", new { code });
        ErrorWhenNull(resp, 400232, "登录信息获取失败");
        ErrorWhen(resp.Scene != scene, 403003, "登录场景不匹配");
        return resp;
    }

    #endregion

    #region 安全验证

    /// <summary>
    /// 获取安全验证代码
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="userId"></param>
    /// <param name="data"></param>
    /// <param name="backUrl"></param>
    /// <returns></returns>
    public SimApiAuthGateDto.GetCodeResponse GetConfirmCode(string scene, string userId,
        Dictionary<string, object>? data = null,
        string? backUrl = null)
    {
        var code = simapi.SignQuery<string>("/api/auth/confirm/code",
            new { scene, data, backUrl, profileId = userId });
        return new SimApiAuthGateDto.GetCodeResponse()
        {
            Code = code!,
            Server = simapi.Server,
            FullUrl = $"{simapi.Server}/confirm?code={code}"
        };
    }

    /// <summary>
    /// 使用安全验证code 获取验证结果
    /// </summary>
    /// <param name="code"></param>
    /// <param name="scene"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    public SimApiAuthGateDto.ConfirmResponse Confirm(string code, string scene, string? userId = null)
    {
        var resp = simapi.SignQuery<SimApiAuthGateDto.ConfirmResponse>("/api/auth/confirm/get", new { code });
        ErrorWhenNull(resp, 403001, "安全确认码无效");
        ErrorWhen(resp.ProfileId != userId, 403002, "安全确认身份不匹配");
        ErrorWhen(resp.Scene != scene, 403003, "安全确认场景不匹配");
        return resp;
    }

    #endregion
}