using Microsoft.Extensions.Logging;
using SimApi.Configurations;
using SimApi.Helpers;

namespace SimApi.AuthSDK;

public class SimApiAuthClient(SimApiOptions apiOptions, ILogger<SimApiAuthClient> logger)
    : SimApiHttpClient(apiOptions, logger)
{
    public override string Server { get; init; } = apiOptions.SimApiAuthGateOptions.Server ?? string.Empty;
    public override string AppId { get; init; } = apiOptions.SimApiAuthGateOptions.AppId ?? string.Empty;
    public override string AppKey { get; init; } = apiOptions.SimApiAuthGateOptions.AppKey ?? string.Empty;
}