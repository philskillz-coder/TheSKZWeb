using TheSKZWeb.Models;

namespace TheSKZWeb.Areas.Users.Models.Home.In
{
    public class In_Login : BaseModel
    {
        public string? UserIdentifier { get; set; }
        public string? Password { get; set; }
    }
}
