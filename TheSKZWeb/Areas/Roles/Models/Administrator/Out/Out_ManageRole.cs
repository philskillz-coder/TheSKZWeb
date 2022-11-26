namespace TheSKZWeb.Areas.Roles.Models.Administrator.Out
{
    public class Out_ManageRole
    {
        public string RoleId { get; set; }
        public bool IsCritical { get; set; }
        public bool IsDefault { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastModifiedAt { get; set; }
    }
}
