namespace TheSKZWeb.Areas.Permissions.Models.Administrator.Out
{
    public class Out_ManagePermission
    {
        public string PermissionId { get; set; }
        public bool IsDefault { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastModifiedAt { get; set; }
    }
}
