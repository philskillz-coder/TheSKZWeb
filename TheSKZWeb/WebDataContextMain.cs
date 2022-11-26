using Microsoft.EntityFrameworkCore;
using TheSKZWeb.Models;

namespace TheSKZWeb;

public class WebDataContextMain : DbContext
{
    public WebDataContextMain(DbContextOptions<WebDataContextMain> options) : base(options)
    {
            
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.UseSerialColumns();
    }

    public DbSet<Permission> Permissions { get; set; }
    
    public DbSet<User> Users { get; set; }
    public DbSet<UserPermission> UserPermissions { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    
    public DbSet<Role> Roles { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }


    public DbSet<ShortLink> ShortLinks { get; set; }
}