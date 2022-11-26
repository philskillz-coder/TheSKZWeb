namespace TheSKZWeb.Areas.Users.Models.Administrator.Out
{
    public class _User
    {
        public string UserId { get; set; }
        public string Username { get; set; }
        public bool CriticalPerms { get; set; }
        public bool Highlighted { get; set; }
    }

    public class Out_ManageUsers
    {
        public IEnumerable<_User> Users { get; set; }
        public _User? Highlighted { get; set; }
    }
}
