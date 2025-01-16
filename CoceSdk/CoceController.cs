using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SimApi.Attributes;
using SimApi.Communications;
using SimApi.Controllers;
using SimApi.Helpers;

namespace SimApi.CoceSdk;

public class CoceController(CoceApp coce, SimApiAuth auth, IServiceProvider sp) : SimApiBaseController
{
    [HttpPost]
    public SimApiBaseResponse<ConfigResponse> GetConfig()
    {
        return new SimApiBaseResponse<ConfigResponse>(coce.GetConfig());
    }

    [HttpPost]
    public SimApiBaseResponse<string> Login([FromBody] SimApiOneFieldRequest<string> request)
    {
        var data = coce.GetLevelToken(request.Data!);
        ErrorWhenNull(data, 400);
        coce.SaveToken(data!.UserId, data.Token);
        var userinfo = coce.GetUserInfo(data!.Token);
        var meta = new Dictionary<string, string>
        {
            { "name", userinfo!.Name },
            { "image", userinfo.Image }
        };
        var groups = coce.GetUserGroups(data.Token!)!;
        var loginItem = new SimApiLoginItem
        {
            Id = data.UserId,
            Meta = meta,
        };
        var processor = sp.GetService<ICoceLoginProcessor>();
        processor?.Process(loginItem, groups.ToArray());
        return new SimApiBaseResponse<string>(auth.Login(loginItem));
    }

    [HttpPost, SimApiAuth]
    public SimApiBaseResponse<GroupInfo[]> ListGroups()
    {
        var levelToken = coce.GetToken(LoginInfo.Id!);
        var groups = coce.GetUserGroups(levelToken!)!;
        return new SimApiBaseResponse<GroupInfo[]>(groups.ToArray());
    }
}