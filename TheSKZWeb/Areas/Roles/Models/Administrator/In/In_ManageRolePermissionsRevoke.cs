using TheSKZWeb.Models;

namespace TheSKZWeb.Areas.Roles.Models.Administrator.In
{
    public class In_ManageRolePermissionsRevoke : BaseModel
    {
        public string? PermissionId { get; set; }
    }
}
