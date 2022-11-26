namespace TheSKZWeb.Areas.Users.Models.Administrator.Out
{
    public class _Role
    {
        public string RoleId { get; set; }
        public string Name { get; set; }
    }
    public class Out_ManageUserRoles
    {
        public string UserId { get; set; }
        public string Username { get; set; }
        public IEnumerable<_Role> Roles { get; set; }
    }
}
