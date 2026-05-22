using Microsoft.Extensions.Logging;
using static SimApi.Helpers.SimApiError;

namespace SimApi.AuthSDK;

public class SimApiAuthIam(SimApiAuthClient simapi, ILogger<SimApiAuthIam> logger)
{
    /// <summary>
    /// 向Iam注册权限
    /// </summary>
    /// <param name="permissions"></param>
    public void RegisterPermissions(SimApiAuthIamDto.PermissionItem[] permissions)
    {
        var log = $"检测到 {permissions.Length} 个权限接口，正在注册..: ";
        foreach (var permission in permissions)
        {
            log += $"\n |- {permission.Identifier} => [{permission.Group}]{permission.Name} ({permission.Description})";
        }

        logger.LogInformation(log);
        simapi.SignQuery<string>("/api/iam/permission/register", new { permissions });
        logger.LogInformation("权限注册完成");
    }

    /// <summary>
    /// 获取拥有的权限标识数组
    /// </summary>
    /// <param name="profileId"></param>
    /// <param name="groupId"></param>
    /// <returns></returns>
    public string[] GetPermissionOwned(string profileId, string? groupId = null)
    {
        return simapi.SignQuery<string[]>("/api/iam/permission/owned", new { profileId, groupId }) ?? [];
    }

    /// <summary>
    /// 检测profileId是否有这个权限
    /// </summary>
    /// <param name="profileId"></param>
    /// <param name="permission"></param>
    /// <param name="groupId"></param>
    public void CheckPermission(string profileId, string permission, string? groupId = null)
    {
        var ok = simapi.SignQuery<bool>("/api/iam/permission/check", new { profileId, permission, groupId });
        ErrorWhen(!ok, 403, "没有该权限");
    }
}