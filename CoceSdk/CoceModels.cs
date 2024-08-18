using System;

namespace SimApi.CoceSdk;

public record ConfigResponse(string AppId, string AuthUrl);

public record LevelTokenResponse(string Token, string UserId, int TokenLevel);

public record GroupInfo(string Id, string Name, string Image, string Description, string Role);

public record UserInfo(string UserId, string Name, string Image);

public record CheckTradeResponse(
    string TradeNo,
    int Amount,
    int Fee,
    string Name,
    string? Ext,
    string Status,
    DateTime CreatedAt,
    DateTime? FinishedAt,
    DateTime? RefundAt,
    DateTime? CloseAt);

public class UserInfoWithGroup
{
    public string? UserId { get; set; }

    public string? Name { get; set; }

    public string? LevelToken { get; set; }

    public UserGroupItem[]? UserGroupItems { get; set; }
}

public class UserGroupItem
{
    public string? GroupId { get; set; }

    public string? GroupName { get; set; }

    public string? GroupRole { get; set; }
}