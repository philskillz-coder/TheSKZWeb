namespace TheSKZWeb.Areas.Users.Models.Administrator.Out
{
    public class _Permission
    {
        public string PermissionId { get; set; }
        public string Name { get; set; }
    }
    public class Out_ManageUserPermissions
    {
        public string UserId { get; set; }
        public string Username { get; set; }
        public IEnumerable<_Permission> Permissions { get; set; }
    }
}
