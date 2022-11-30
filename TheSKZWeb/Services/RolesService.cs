using HashidsNet;
using Microsoft.EntityFrameworkCore;
using TheSKZWeb.Models;

namespace TheSKZWeb.Services
{

    public interface IRolesService
    {
        IQueryable<Role> GetAllRoles();
        
        Task<Role?> GetRoleByIndex(int index);
        IQueryable<Role> GetRolesByIndex(int[] indexes);

        Task<Role?> GetRoleByName(string name);
        IQueryable<Role> GetRolesByName(string[] names);

        IQueryable<Role> GetDefaultRoles();
        Task<Role> CreateRole(string name, bool isDefault);
        Task<bool> RoleNameExists(string name);

        string GetRoleHashId(int roleId);
        bool GetRoleId(string roleHashId, out int roleId);
        Task<Role?> GetRoleFromPartial(string? partial);
        Task DeleteRole(Role role);
        Task UpdateRole(Role role);
        IQueryable<Permission> GetAllRolePermissions(int role);

        Task GrantPermission(Role role, Permission permission);
        Task RevokePermission(Role role, Permission permission);

        //bool IncludesRoles(object[] presentRoles, object[] requiredRoles);

    }

    public class RolesService : IRolesService
    {
        private readonly WebDataContextMain _db;
        private readonly Hashids _roleHashids;

        public RolesService(WebDataContextMain db)
        {
            _db = db;
            _roleHashids = new Hashids(
                "PpMPXTqDzxawOvrxToTjsUTsVqSFfJvg",
                8
            );

        }

        public async Task<Role?> GetRoleByIndex(int index)
        {
            return await _db.Roles.FirstOrDefaultAsync(r => r.Id == index);
        }

        public IQueryable<Role> GetRolesByIndex(int[] indexes)
        {
            return _db.Roles.Where(r => indexes.Contains(r.Id));
        }

        public IQueryable<Role> GetDefaultRoles()
        {
            return _db.Roles.Where(r => r.IsDefault == true);
        }

        public IQueryable<Role> GetAllRoles()
        {
            return _db.Roles;
        }

        public async Task<Role> CreateRole(string name, bool isDefault)
        {
            Role createdRole = new Role
            {
                Name = name,
                IsDefault = isDefault,
                CreatedAt = DateTime.UtcNow
            };

            await _db.Roles.AddAsync(createdRole);
            await _db.SaveChangesAsync();

            return createdRole;
        }

        public async Task<bool> RoleNameExists(string name)
        {
            return await _db.Roles.AnyAsync(r => r.Name == name);
        }


        public string GetRoleHashId(int roleId)
        {
            return _roleHashids.Encode(roleId);
        }

        public bool GetRoleId(string roleHashId, out int roleId)
        {
            var hid = _roleHashids.Decode(roleHashId);
            roleId = hid.Length > 0 ? hid[0] : -1;

            return roleId >= 0;
        }

        public async Task<Role?> GetRoleByName(string name)
        {
            return await _db.Roles.FirstOrDefaultAsync(r => r.Name == name);
        }

        public IQueryable<Role> GetRolesByName(string[] names)
        {
            return _db.Roles.Where(r => names.Contains(r.Name));
        }

        public async Task<Role?> GetRoleFromPartial(string? partial)
        {
            if (partial == null)
            {
                return null;
            }

            Role? role = null;
            if (partial.StartsWith("@"))
            {
                role = await GetRoleByName(partial.Substring(1));
            }
            else if (GetRoleId(partial, out int roleId))
            {
                role = await GetRoleByIndex(roleId);
            }

            return role;
        }

        public async Task DeleteRole(Role role)
        {
            _db.Roles.Remove(role);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateRole(Role role)
        {
            role.LastModifiedAt = DateTime.UtcNow;

            _db.Update(role);

            await _db.SaveChangesAsync();
        }

        public IQueryable<Permission> GetAllRolePermissions(int role)
        {
            return
                _db
                .RolePermissions
                .Include(r => r.Role)
                .Where(r => r.Role.Id == role)
                .Include(r => r.Permission)
                .Select(r => r.Permission)
                ;

        }

        public async Task GrantPermission(Role role, Permission permission)
        {
            await _db.RolePermissions.AddAsync(new RolePermission
            {
                Role = role,
                Permission = permission
            });
            await _db.SaveChangesAsync();
        }

        public async Task RevokePermission(Role role, Permission permission)
        {
            RolePermission? rolePermission = await _db.RolePermissions.FirstOrDefaultAsync(
                up =>
                up.Role.Id == role.Id
                &&
                up.Permission.Id == permission.Id
            );

            if (rolePermission == null)
            {
                throw new Exception("Permission not found");
            }

            _db.RolePermissions.Remove(rolePermission);

            await _db.SaveChangesAsync();
        }

        //public bool IncludesRoles(object[] presentRoles, object[] requiredRoles)
        //{
        //    return requiredRoles.Intersect(presentRoles).Count() == requiredRoles.Count();
        //}
    }
}
