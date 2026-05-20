using System;
using System.Collections.Generic;

namespace SimApi.AuthSDK;

public class SimApiAuthCenterDto
{
    public class AppAndProfileItem
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public string? Image { get; set; }
        public string? Description { get; set; }
    }

    public class ConfirmResponse
    {
        public required string ApplicationId { get; set; }
        public required string ProfileId { get; set; }
        public string? Scene { get; set; }
        public Dictionary<string, object>? Data { get; set; }
    }

    public class LoginInfoResponse
    {
        public string? Scene { get; set; }
        public Dictionary<string, object>? Data { get; set; }
        public required string ProfileId { get; set; }
        public required string Name { get; set; }
        public string? Image { get; set; }
        public string? Description { get; set; }
    }

    public class GetCodeResponse
    {
        public required string Code { get; set; }
        public required string Server { get; set; }
        public required string FullUrl { get; set; }
    }

    public class GroupRelatedItem
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public string? Image { get; set; }
        public string? Description { get; set; }
        public bool IsOwner { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsMember { get; set; }
    }

    public class GroupDetailTreeNode
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public string? Image { get; set; }
        public string? Description { get; set; }
        public int Sort { get; set; }
        public List<GroupDetailTreeNode> Children { get; set; } = [];
    }
}