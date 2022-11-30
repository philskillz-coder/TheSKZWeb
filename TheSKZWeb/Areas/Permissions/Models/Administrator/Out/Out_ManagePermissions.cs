namespace TheSKZWeb.Areas.Permissions.Models.Administrator.Out;

public class _Permission
{
    public string PermissionId { get; set; }
    public bool IsDefault { get; set; }
    public string Name { get; set; }
    public bool Highlighted { get; set; }
}

public class Out_ManagePermissions
{
    public IEnumerable<_Permission> Permissions { get; set; }
    public _Permission? Highlighted { get; set; }
}
