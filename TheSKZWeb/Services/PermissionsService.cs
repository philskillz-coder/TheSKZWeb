using HashidsNet;
using Microsoft.EntityFrameworkCore;
using TheSKZWeb.Models;

namespace TheSKZWeb.Services
{

    public interface IPermissionsService
    {
        IQueryable<Permission> GetAllPermissions();
        
        Task<Permission?> GetPermissionById(int id);
        IQueryable<Permission> GetPermissionsById(params int[] ids);

        Task<Permission?> GetPermissionByName(string name);
        IQueryable<Permission> GetPermissionsByName(string[] names);

        IQueryable<Permission> GetDefaultPermissions();
        Task<Permission> CreatePermission(string name, bool isDefault);
        Task<bool> PermissionNameExists(string name);

        //bool IncludesPermissions(IEnumerable<object> presentPermissions, IEnumerable<object> requiredPermissions);

        string GetPermissionHashId(int permissionId);
        bool GetPermissionId(string permissionHashId, out int permissionId);
        Task<Permission?> GetPermissionFromPartial(string? partial);
        Task DeletePermission(Permission permission);
        Task UpdatePermission(Permission permission);

    }

    public class PermissionsService : IPermissionsService
    {
        private readonly WebDataContextMain _db;
        private readonly Hashids _permissionHashids;

        public PermissionsService(WebDataContextMain db)
        {
            _db = db;
            _permissionHashids = new Hashids(
                "{F8C0E954-7FA4-46D2-83A1-398C93F3F3B6}",
                8
            );

        }

        public async Task<Permission?> GetPermissionById(int id)
        {
            return await _db.Permissions.FirstOrDefaultAsync(p => p.Id == id);
        }

        public IQueryable<Permission> GetPermissionsById(int[] id)
        {
            return _db.Permissions.Where(p => id.Contains(p.Id));
        }

        public IQueryable<Permission> GetDefaultPermissions()
        {
            return _db.Permissions.Where(p => p.IsDefault == true);
        }

        public IQueryable<Permission> GetAllPermissions()
        {
            return _db.Permissions;
        }

        public async Task<Permission> CreatePermission(string name, bool isDefault)
        {
            Permission createdPermission = new Permission
            {
                Name = name,
                IsDefault = isDefault,
                CreatedAt = DateTime.UtcNow
            };

            await _db.Permissions.AddAsync(createdPermission);
            await _db.SaveChangesAsync();

            return createdPermission;
        }

        public async Task<bool> PermissionNameExists(string name)
        {
            return await _db.Permissions.AnyAsync(p => p.Name == name);
        }


        public string GetPermissionHashId(int permissionId)
        {
            return _permissionHashids.Encode(permissionId);
        }

        public bool GetPermissionId(string permissionHashId, out int permissionId)
        {
            var hid = _permissionHashids.Decode(permissionHashId);
            permissionId = hid.Length > 0 ? hid[0] : -1;

            return permissionId >= 0;
        }

        public async Task<Permission?> GetPermissionByName(string name)
        {
            return await _db.Permissions.FirstOrDefaultAsync(p => p.Name == name);
        }

        public IQueryable<Permission> GetPermissionsByName(string[] names)
        {
            return _db.Permissions.Where(p => names.Contains(p.Name));
        }

        public async Task<Permission?> GetPermissionFromPartial(string? partial)
        {
            if (partial == null)
            {
                return null;
            }

            Permission? permission = null;
            if (partial.StartsWith("@"))
            {
                permission = await GetPermissionByName(partial.Substring(1));
            }
            else if (GetPermissionId(partial, out int permissionId))
            {
                permission = await GetPermissionById(permissionId);
            }

            return permission;
        }

        public async Task DeletePermission(Permission permission)
        {
            _db.Permissions.Remove(permission);
            await _db.SaveChangesAsync();
        }

        public async Task UpdatePermission(Permission permission)
        {
            permission.LastModifiedAt = DateTime.UtcNow;

            _db.Update(permission);

            await _db.SaveChangesAsync();
        }
    }
}
