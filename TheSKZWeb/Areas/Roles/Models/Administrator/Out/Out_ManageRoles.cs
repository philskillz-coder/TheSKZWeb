namespace TheSKZWeb.Areas.Roles.Models.Administrator.Out;

public class _Role
{
    public string RoleId { get; set; }
    public bool IsDefault { get; set; }
    public string Name { get; set; }
    public bool Highlighted { get; set; }
}

public class Out_ManageRoles
{
    public IEnumerable<_Role> Roles { get; set; }
    public _Role? Highlighted { get; set; }
}
