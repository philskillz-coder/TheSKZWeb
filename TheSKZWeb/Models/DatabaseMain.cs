using System.ComponentModel.DataAnnotations;

namespace TheSKZWeb.Models;


public class Permission
{
    [Key] public int Id { get; set; }

    [Required] public bool IsDefault { get; set; }


    //[Index(IsUnique = true)]
    // only since efc6.1 (this is 6.0)
    [Required]
    public string Name { get; set; }
    
    [Required] public DateTime CreatedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }
}

public class Role
{
    [Key] public int Id { get; set; }

    [Required] public bool IsDefault { get; set; }

    [Required] public string Name { get; set; }

    [Required] public DateTime CreatedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }


    
}

public class RolePermission
{
    [Key] public int Id { get; set; }
    [Required] public Role Role { get; set; }
    [Required] public Permission Permission { get; set; }
    [Required] public DateTime CreatedAt { get; set; }
}

public class User
{
    [Key] public int Id { get; set; }
    [Required] public string Username { get; set; }
    
    [Required] public byte[] PasswordHash { get; set; }
    [Required] public byte[] PasswordSalt { get; set; }

    [Required] public byte[] RecoveryTokenHash { get; set; }
    [Required] public byte[] RecoveryTokenSalt { get; set; }

    [Required] public bool MFAEnabled { get; set; } = false;

    [Required] public DateTime CreatedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }
}

public class UserPermission
{
    [Key] public int Id { get; set; }
    [Required] public User User { get; set; }
    [Required] public Permission Permission { get; set; }
    [Required] public DateTime CreatedAt { get; set; }

}

public class UserRole
{
    [Key] public int Id { get; set; }
    [Required] public User User { get; set; }
    [Required] public Role Role { get; set; }
    [Required] public DateTime CreatedAt { get; set; }

}

public class ShortLink
{
    [Key] public int Id { get; set; }
    [Required] public User Owner { get; set; }
    [Required] public string Name { get; set; }
    [Required] public string Target { get; set; }

    [Required] public bool Private { get; set; }
    [Required] public bool PasswordProtected { get; set; }
    public byte[]? Password { get; set; }
}