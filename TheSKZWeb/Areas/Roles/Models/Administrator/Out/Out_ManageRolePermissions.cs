namespace TheSKZWeb.Areas.Roles.Models.Administrator.Out
{
    public class _Permission
    {
        public string PermissionId { get; set; }
        public string Name { get; set; }
    }
    public class Out_ManageRolePermissions
    {
        public string RoleId { get; set; }
        public string Rolename { get; set; }
        public IEnumerable<_Permission> Permissions { get; set; }
    }
}
