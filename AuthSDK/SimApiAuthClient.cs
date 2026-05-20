using Microsoft.Extensions.Logging;
using SimApi.Configurations;
using SimApi.Helpers;

namespace SimApi.AuthSDK;

public class SimApiAuthClient(SimApiOptions apiOptions, ILogger<SimApiAuthClient> logger)
    : SimApiHttpClient(apiOptions, logger)
{
    public override string Server { get; init; } = apiOptions.SimApiAuthCenterOptions.Server ?? string.Empty;
    public override string AppId { get; init; } = apiOptions.SimApiAuthCenterOptions.AppId ?? string.Empty;
    public override string AppKey { get; init; } = apiOptions.SimApiAuthCenterOptions.AppKey ?? string.Empty;
}