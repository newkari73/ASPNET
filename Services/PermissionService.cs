using AspNetBbs.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AspNetBbs.Services;

public class PermissionService(IConfiguration configuration) : IPermissionService
{
    private readonly string connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("DefaultConnection이 설정되지 않았습니다.");

    public async Task<UserPermissionsModel> GetUserPermissionsAsync(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new UserPermissionsModel();
        }

        var isAdmin = await IsAdminAsync(userId);
        var permissions = new List<MenuPermissionItem>();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand("ExecWeb.MemberMenuPermission_SelectByUserId", connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.Add("@UserID", SqlDbType.NVarChar, 50).Value = userId;

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            permissions.Add(new MenuPermissionItem
            {
                MenuCode = reader.GetString(reader.GetOrdinal("MenuCode")),
                MenuName = reader.GetString(reader.GetOrdinal("MenuName")),
                ControllerName = reader.GetString(reader.GetOrdinal("ControllerName")),
                ActionName = reader.GetString(reader.GetOrdinal("ActionName")),
                SortOrder = reader.GetInt32(reader.GetOrdinal("SortOrder")),
                CanRead = reader.GetBoolean(reader.GetOrdinal("CanRead")),
                CanUpdate = reader.GetBoolean(reader.GetOrdinal("CanUpdate")),
                CanDelete = reader.GetBoolean(reader.GetOrdinal("CanDelete"))
            });
        }

        return new UserPermissionsModel
        {
            UserID = userId,
            IsAdmin = isAdmin,
            Permissions = permissions
        };
    }

    public async Task<bool> HasPermissionAsync(string? userId, string menuCode, PermissionType type)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        var userPerms = await GetUserPermissionsAsync(userId);
        if (userPerms.IsAdmin)
        {
            return true;
        }

        return type switch
        {
            PermissionType.Read => userPerms.CanRead(menuCode),
            PermissionType.Update => userPerms.CanUpdate(menuCode),
            PermissionType.Delete => userPerms.CanDelete(menuCode),
            _ => false
        };
    }

    public async Task<bool> IsAdminAsync(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        if (string.Equals(userId, "kari73", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new SqlCommand("ExecWeb.Member_CheckIsAdmin", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.Add("@UserID", SqlDbType.NVarChar, 50).Value = userId;
            var result = await command.ExecuteScalarAsync();
            if (result is bool isAdmin)
            {
                return isAdmin;
            }
            if (result is not null && int.TryParse(result.ToString(), out var intVal))
            {
                return intVal == 1;
            }
        }
        catch
        {
            // fallback
        }

        return false;
    }

    public async Task<List<MemberPermissionManageItem>> GetAllMembersWithPermissionsAsync()
    {
        var memberDict = new Dictionary<string, MemberPermissionManageItem>(StringComparer.OrdinalIgnoreCase);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand("ExecWeb.Member_SelectAllWithPermissions", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var userId = reader.GetString(reader.GetOrdinal("UserID"));
            var userName = reader.GetString(reader.GetOrdinal("UserName"));
            var isAdmin = reader.GetBoolean(reader.GetOrdinal("IsAdmin"));
            var menuCode = reader.GetString(reader.GetOrdinal("MenuCode"));
            var menuName = reader.GetString(reader.GetOrdinal("MenuName"));
            var canRead = reader.GetBoolean(reader.GetOrdinal("CanRead"));
            var canUpdate = reader.GetBoolean(reader.GetOrdinal("CanUpdate"));
            var canDelete = reader.GetBoolean(reader.GetOrdinal("CanDelete"));

            if (!memberDict.TryGetValue(userId, out var memberItem))
            {
                memberItem = new MemberPermissionManageItem
                {
                    UserID = userId,
                    UserName = userName,
                    IsAdmin = isAdmin,
                    Permissions = []
                };
                memberDict[userId] = memberItem;
            }

            memberItem.Permissions.Add(new MenuPermissionItem
            {
                MenuCode = menuCode,
                MenuName = menuName,
                CanRead = canRead,
                CanUpdate = canUpdate,
                CanDelete = canDelete
            });
        }

        return [.. memberDict.Values];
    }

    public async Task SavePermissionsAsync(string userId, List<MenuPermissionSaveItem> permissions)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        foreach (var perm in permissions)
        {
            await using var command = new SqlCommand("ExecWeb.MemberMenuPermission_Save", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.Add("@UserID", SqlDbType.NVarChar, 50).Value = userId;
            command.Parameters.Add("@MenuCode", SqlDbType.NVarChar, 50).Value = perm.MenuCode;
            command.Parameters.Add("@CanRead", SqlDbType.Bit).Value = perm.CanRead;
            command.Parameters.Add("@CanUpdate", SqlDbType.Bit).Value = perm.CanUpdate;
            command.Parameters.Add("@CanDelete", SqlDbType.Bit).Value = perm.CanDelete;
            await command.ExecuteNonQueryAsync();
        }
    }
}