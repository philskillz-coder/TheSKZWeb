namespace TheSKZWeb.Areas.Users.Models.Home.Out
{
    public class Out_Register
    {
        public string UserHashId { get; set; }
        public string Username { get; set; }
        public List<string> Permissions { get; set; }
        public string RecoveryToken { get; set; }
    }
}
