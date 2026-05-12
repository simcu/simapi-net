using System;
using System.Collections.Generic;

namespace SimApi.AuthGate;

public class SimApiAuthGateDto
{
    public class AppAndProfileItem
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
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

    public class AuthInfoResponse
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
}