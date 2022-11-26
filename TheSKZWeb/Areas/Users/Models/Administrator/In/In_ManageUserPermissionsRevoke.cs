using TheSKZWeb.Models;

namespace TheSKZWeb.Areas.Users.Models.Administrator.In
{
    public class In_ManageUserPermissionsRevoke : BaseModel
    {
        public string? PermissionId { get; set; }
    }
}
