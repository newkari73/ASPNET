using AspNetBbs.Models;

namespace AspNetBbs.Services;

public interface IPermissionService
{
    Task<UserPermissionsModel> GetUserPermissionsAsync(string? userId);
    Task<bool> HasPermissionAsync(string? userId, string menuCode, PermissionType type);
    Task<bool> IsAdminAsync(string? userId);
    Task<List<MemberPermissionManageItem>> GetAllMembersWithPermissionsAsync();
    Task SavePermissionsAsync(string userId, List<MenuPermissionSaveItem> permissions);
}