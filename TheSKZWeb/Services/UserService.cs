using HashidsNet;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using TheSKZWeb.AuthorizationPolicies;
using TheSKZWeb.Models;
using System.Collections.Generic;

namespace TheSKZWeb.Services
{
    class ds<T>
    {

    }

    public interface IUserService
    { 
        void CreateHash(string valuePlain, out byte[] valueHash, out byte[] valueSalt);
        bool VerifyHash(string? valuePlain, byte[]? valueHash, byte[]? valueSalt);


        string CreateJWT(string securityToken, string issuer, string audience, string username, string userHashId);
        bool CreateRecoveryToken(int length, out string recoveryTokenPlain, out byte[] recoveryTokenHash, out byte[] recoveryTokenSalt);


        IQueryable<User> GetAllUsers();
        Task<User?> GetUserById(int id);
        Task<User?> GetUserByUsername(string username);
        Task<User?> GetUserByPartial(string? partial);
        
        string GetUserHashId(int userId);
        bool GetUserId(string userHashId, out int userId);


        Task<User> CreateUser(string username, string password, byte[] recoveryTokenHash, byte[] recoveryTokenSalt);
        Task UpdateUser(User user);
        Task DeleteUser(User user);
        
        Task<bool> UsernameExists(string Username);


        IQueryable<Permission> GetAllUserPermissions(int user); // Permissions from all roles and userspecific permissions combined
        IQueryable<Permission> GetUserPermissions(int user);
        Task GrantPermission(User user, Permission permission);
        Task RevokePermission(User user, Permission permission);

        Task<bool> HasPermission<T>(User? user, Func<Permission, T> selector, params T[] permissions);
        

        IQueryable<Role> GetUserRoles(int user);
        Task GrantRole(User user, Role role);
        Task RevokeRole(User user, Role role);

        Task<bool> HasRole<T>(User? user, Func<Role, T> selector, params T[] roles);


        ClaimsPrincipal CreateLoginPrincipal(User user);
    }

    public class UserService : IUserService
    {
        private readonly WebDataContextMain _db;
        private readonly IPermissionsService _permissionsService;
        private readonly IRolesService _rolesService;
        private readonly Hashids _userHashids;

        public UserService(WebDataContextMain db, IPermissionsService permissionsService, IRolesService rolesService)
        {
            _db = db;
            _permissionsService = permissionsService;
            _userHashids = new Hashids("CRYKJ9ErwvsbPhjLoiW90uJ2rGEqVCqC", 8);
            _rolesService = rolesService;
        }


        public void CreateHash(string valuePlain, out byte[] valueHash, out byte[] valueSalt)
        {
            using (HMACSHA512 hmac = new HMACSHA512())
            {
                valueSalt = hmac.Key;
                valueHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(valuePlain));
            }
        }

        public bool VerifyHash(string? valuePlain, byte[]? valueHash, byte[]? valueSalt)
        {
            if (valuePlain == null || valueHash == null || valueSalt == null)
            {
                return false;
            }

            using (HMACSHA512 hmac = new HMACSHA512(valueSalt))
            {
                byte[] computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(valuePlain));
                return computedHash.SequenceEqual(valueHash);
            }
        }

        public string CreateJWT(string securityToken, string issuer, string audience, string username, string userHashId)
        {
            SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityToken));

            SigningCredentials creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            JwtSecurityToken token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: new List<Claim> {
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.NameIdentifier, userHashId)
                },
                expires: DateTime.UtcNow.AddDays(90),
                signingCredentials: creds
            ); ;

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public bool CreateRecoveryToken(int length, out string recoveryTokenPlain, out byte[] recoveryTokenHash, out byte[] recoveryTokenSalt)
        {
            recoveryTokenPlain = Convert.ToBase64String(RandomNumberGenerator.GetBytes(length));

            CreateHash(recoveryTokenPlain, out recoveryTokenHash, out recoveryTokenSalt);

            return true;
        }



        public IQueryable<User> GetAllUsers()
        {
            return _db.Users;
        }

        public async Task<User?> GetUserById(int userId)
        {
            return await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<User?> GetUserByUsername(string username)
        {
            return await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        }
        
        public async Task<User?> GetUserByPartial(string? partial)
        {
            if (partial == null)
            {
                return null;
            }

            User? user = null;
            if (partial.StartsWith("@"))
            {
                user = await GetUserByUsername(partial.Substring(1));
            } else if (GetUserId(partial, out int userId)) {
                user = await GetUserById(userId);
            }

            return user;
        }


        public string GetUserHashId(int userId)
        {
            return _userHashids.Encode(userId);
        }

        public bool GetUserId(string userHashId, out int userId)
        {
            var hid = _userHashids.Decode(userHashId);
            userId = hid.Length > 0 ? hid[0] : -1;

            return userId >= 0;
        }


        public async Task<User> CreateUser(
            string username,
            string password,
            byte[] recoveryTokenHash,
            byte[] recoveryTokenSalt
        )
        {
            CreateHash(password, out byte[] passwordHash, out byte[] passwordSalt);

            DateTime now = DateTime.UtcNow;

            User user = new User()
            {
                Username = username,

                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,

                RecoveryTokenHash = recoveryTokenHash,
                RecoveryTokenSalt = recoveryTokenSalt,

                CreatedAt = now
            };

            await _db.Users.AddAsync(user);
            
            await _db.UserRoles.AddRangeAsync(
                (
                    from role
                    in await _rolesService.GetDefaultRoles().ToArrayAsync()
                    select 
                        new UserRole { 
                            User = user,
                            Role = role,
                            CreatedAt = now
                        }
                )
            );

            await _db.UserPermissions.AddRangeAsync(
                (
                    from permission
                    in await _permissionsService.GetDefaultPermissions().ToArrayAsync()
                    select
                        new UserPermission { 
                            User = user,
                            Permission = permission,
                            CreatedAt = now
                        }
                )
            );

            await _db.SaveChangesAsync();
            
            return user;
        }

        public async Task UpdateUser(User user)
        {
            user.LastModifiedAt = DateTime.UtcNow;
            _db.Users.Update(user);

            await _db.SaveChangesAsync();
        }

        public async Task DeleteUser(User user)
        {
            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
        }


        public async Task<bool> UsernameExists(string Username)
        {
            return await _db.Users.AnyAsync(u => u.Username == Username);
        }

        
        public IQueryable<Permission> GetAllUserPermissions(int user)
        {
            return 
                _db
                .UserRoles
                .Where(userRole => userRole.User.Id == user)
                .Include(userRole => userRole.Role)
                .Select(userRole => userRole.Role)

                .Join(
                    _db.RolePermissions,
                    userRole => userRole,
                    rolePermission => rolePermission.Role,
                    (userRole, rolePermission) => rolePermission.Permission
                )
                .Union(
                    _db
                    .UserPermissions
                    .Where(userPermission => userPermission.User.Id == user)
                    .Include(userPermission => userPermission.Permission)
                    .Select(userPermission => userPermission.Permission)
                );
        }

        public IQueryable<Permission> GetUserPermissions(int user)
        {
            return _db.UserPermissions.Where(p => p.User.Id == user).Include(p => p.Permission).Select(p => p.Permission);
        }

        public async Task GrantPermission(User user, Permission permission)
        {
            await _db.UserPermissions.AddAsync(new UserPermission
            {
                User = user,
                Permission = permission,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
        }

        public async Task RevokePermission(User user, Permission permission)
        {
            UserPermission? userPermission = await _db.UserPermissions.FirstOrDefaultAsync(
                up =>
                up.User.Id == user.Id
                &&
                up.Permission.Id == permission.Id
            );

            if (userPermission == null)
            {
                throw new Exception("permission not found");
            }

            _db.UserPermissions.Remove(userPermission);

            await _db.SaveChangesAsync();
        }


        public async Task<bool> HasPermission<T>(User? user, Func<Permission, T> selector, params T[] requiredPermissions)
        {
            if (user == null)
            {
                return false;
            }

            Permission[] userPermissions = await GetAllUserPermissions(user.Id).ToArrayAsync();

            var result = from one in userPermissions
                         join two in requiredPermissions on selector(one) equals two
                         select one;

            return requiredPermissions.Count() == result.Count();
        }



        public IQueryable<Role> GetUserRoles(int user)
        {
            return _db.UserRoles.Where(r => r.User.Id == user).Include(r => r.Role).Select(r => r.Role);
        }

        public async Task GrantRole(User user, Role role)
        {
            await _db.UserRoles.AddAsync(new UserRole
            {
                User = user,
                Role = role,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
        }

        public async Task RevokeRole(User user, Role role)
        {
            UserRole? userRole = await _db.UserRoles.FirstOrDefaultAsync(
                up =>
                up.User.Id == user.Id
                &&
                up.Role.Id == role.Id
            );

            if (userRole == null)
            {
                throw new Exception("Role not found");
            }

            _db.UserRoles.Remove(userRole);

            await _db.SaveChangesAsync();
        }

        public async Task<bool> HasRole<T>(User? user, Func<Role, T> selector, params T[] requiredRoles)
        {
            if (user == null)
            {
                return false;
            }

            Role[] userRoles = await GetUserRoles(user.Id).ToArrayAsync();

            var result = from one in userRoles
                         join two in requiredRoles on selector(one) equals two
                         select one;

            return requiredRoles.Count() == result.Count();
        }

        
        

        public ClaimsPrincipal CreateLoginPrincipal(User user)
        {
            ClaimsIdentity identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);

            identity.AddClaim(new Claim(ClaimTypes.Name, user.Username));
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, GetUserHashId(user.Id)));
            
            return new ClaimsPrincipal(identity);
        }
        
    }
}
