namespace AspNetBbs.Models;

public enum PermissionType
{
    Read,
    Update,
    Delete
}

public class MenuPermissionItem
{
    public string MenuCode { get; set; } = string.Empty;
    public string MenuName { get; set; } = string.Empty;
    public string ControllerName { get; set; } = string.Empty;
    public string ActionName { get; set; } = "Index";
    public int SortOrder { get; set; }
    public bool CanRead { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanDelete { get; set; }
}

public class UserPermissionsModel
{
    public string UserID { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public List<MenuPermissionItem> Permissions { get; set; } = [];

    public bool CanRead(string menuCode)
    {
        if (IsAdmin) return true;
        var perm = Permissions.FirstOrDefault(p => p.MenuCode.Equals(menuCode, StringComparison.OrdinalIgnoreCase));
        return perm?.CanRead ?? false;
    }

    public bool CanUpdate(string menuCode)
    {
        if (IsAdmin) return true;
        var perm = Permissions.FirstOrDefault(p => p.MenuCode.Equals(menuCode, StringComparison.OrdinalIgnoreCase));
        return perm?.CanUpdate ?? false;
    }

    public bool CanDelete(string menuCode)
    {
        if (IsAdmin) return true;
        var perm = Permissions.FirstOrDefault(p => p.MenuCode.Equals(menuCode, StringComparison.OrdinalIgnoreCase));
        return perm?.CanDelete ?? false;
    }
}

public class MemberPermissionManageItem
{
    public string UserID { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public List<MenuPermissionItem> Permissions { get; set; } = [];
}

public class PermissionManageViewModel
{
    public List<MemberPermissionManageItem> Members { get; set; } = [];
    public string? SelectedUserId { get; set; }
    public MemberPermissionManageItem? SelectedMember =>
        Members.FirstOrDefault(m => m.UserID.Equals(SelectedUserId, StringComparison.OrdinalIgnoreCase))
        ?? Members.FirstOrDefault();
}

public class MenuPermissionSaveItem
{
    public string MenuCode { get; set; } = string.Empty;
    public bool CanRead { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanDelete { get; set; }
}

public class PermissionSaveRequest
{
    public string UserID { get; set; } = string.Empty;
    public List<MenuPermissionSaveItem> Permissions { get; set; } = [];
}