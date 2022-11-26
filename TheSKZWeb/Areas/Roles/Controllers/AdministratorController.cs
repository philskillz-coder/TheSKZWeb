using Microsoft.AspNetCore.Mvc;
using TheSKZWeb.AuthorizationPolicies;
using TheSKZWeb.Overwrites;
using TheSKZWeb.Services;

using TheSKZWeb.Areas.Roles.Models.Administrator.In;
using TheSKZWeb.Areas.Roles.Models.Administrator.Out;
using TheSKZWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace TheSKZWeb.Areas.Roles.Controllers
{
    [Area("Roles")]
    [Permission(
        ServicePermissions.Roles,
        ServicePermissions.Roles_Administrator
    )]
    public class AdministratorController : NewController
    {
        private readonly IRolesService _rolesService;
        private readonly IPermissionsService _permissionsService;
        private readonly IMFAService _mfaService;

        public AdministratorController(IRolesService rolesService, IPermissionsService permissionsService, IUserService userService, IMFAService mfaService) : base(userService)
        {
            _rolesService = rolesService;
            _permissionsService = permissionsService;
            _mfaService = mfaService;
        }


        [HttpGet("/administrator/roles")]
        public async Task<IActionResult> ManageRoles(
            [FromQuery] In_ManageRoles request
        )
        {
            Role? highlightedRole = await _rolesService.GetRoleFromPartial(request.Highlighted);

            IEnumerable<_Role> roles = (
                from role
                in (await _rolesService.GetAllRoles().OrderBy(r => r.Name).ToArrayAsync())
                select new _Role
                {
                    RoleId = _rolesService.GetRoleHashId(role.Id),
                    IsCritical = role.IsCritical,
                    IsDefault = role.IsDefault,
                    Name = role.Name,
                    Highlighted = role.Id == highlightedRole?.Id
                }
            );

            return View(new Out_ManageRoles
            {
                Roles = roles,
                Highlighted = highlightedRole != null ? new _Role
                {
                    RoleId = _rolesService.GetRoleHashId(highlightedRole.Id),
                    IsCritical = highlightedRole.IsCritical,
                    IsDefault = highlightedRole.IsDefault,
                    Name = highlightedRole.Name,
                    Highlighted = true
                } : null
            });
        }

        [HttpGet("/administrator/roles/create")]
        [Permission(
            ServicePermissions.Roles_Administrator_Create
        )]
        public IActionResult ManageRolesCreate()
        {
            return View();
        }

        [HttpPost("/administrator/roles/create")]
        [Permission(
            ServicePermissions.Roles_Administrator_Create
        )]
        public async Task<IActionResult> ManageRolesCreate(
            [FromBody] In_ManageRoleCreate request
        )
        {
            if (request.Name == null || request.Critical == null || request.Default == null)
            {
                return Ok(
                    new
                    {
                        Message = "Missing Data",
                        For = "Manage Roles Create",
                        Type = "Error"
                    }
                );
            }

            if (await _rolesService.RoleNameExists(request.Name))
            {
                return Ok(
                    new
                    {
                        Message = "Role name already exists",
                        For = "Manage Roles Create",
                        Type = "Error",
                        Data = new
                        {
                            RoleName = request.Name
                        }
                    }
                );
            }


            Role createdRole = await _rolesService.CreateRole(request.Name, request.Critical ?? false, request.Default ?? false);

            User? identity = await GetIdentity();
            if (identity == null)
            {
                return BadRequest();
            }

            await _userService.GrantRole(
                identity,
                createdRole
            );

            return Ok(
                new
                {
                    Message = "Role Created",
                    For = "Manage Roles Create",
                    Type = "Success",
                    Data = new
                    {
                        RoleIdentifier = $"@{createdRole.Name}"
                    },
                    JumpTo = Url.Action(
                        action: "ManageRoles",
                        controller: "Administrator",
                        values: new
                        {
                            Area = "Roles",
                            Highlighted = $"@{createdRole.Name}"
                        }
                    )
                }
            );
        }

        [HttpGet("/administrator/roles/{roleId}")]
        [Permission(
            ServicePermissions.Roles_Administrator_Role
        )]
        public async Task<IActionResult> ManageRole(
            [FromRoute] In_PartialRoleIdentifier roleIdentifier
        )
        {
            Role? role = await _rolesService.GetRoleFromPartial(roleIdentifier.roleId);
            if (role == null)
            {
                return Ok(
                    new
                    {
                        Message = "Role not found",
                        For = "Manage Role",
                        Type = "Error",
                        Data = new
                        {
                            RoleIdentifier = roleIdentifier.roleId
                        }
                    }
                );
            }

            return View(new Out_ManageRole
            {
                RoleId = _rolesService.GetRoleHashId(role.Id),
                IsCritical = role.IsCritical,
                IsDefault = role.IsDefault,
                Name = role.Name,
                CreatedAt = role.CreatedAt,
                LastModifiedAt = role.LastModifiedAt
            });
        }
        // 4x "/" => summary


        // allways 2fa
        [HttpPost("/administrator/roles/{roleId}/delete")]
        [Permission(
            ServicePermissions.Roles_Administrator_Role,
            ServicePermissions.Roles_Administrator_Role_Delete
        )]
        public async Task<IActionResult> ManageRoleDelete(
            [FromRoute] In_PartialRoleIdentifier roleIdentifier,
            [FromBody] In_ManageRoleDelete request
        )
        {
            Role? role = await _rolesService.GetRoleFromPartial(roleIdentifier.roleId);
            if (role == null)
            {
                return Ok(
                    new
                    {
                        Message = "Role not found",
                        For = "Manage Role Delete",
                        Type = "Error",
                        Data = new
                        {
                            RoleIdentifier = roleIdentifier.roleId
                        }
                    }
                );
            }

            User? identity = await GetIdentity();
            if (identity == null)
            {
                return BadRequest();
            }

            var identityHId = _userService.GetUserHashId(identity.Id);
            if (request.mfaCode == null)
            {
                return Ok(
                    new
                    {
                        Message = "2FA Validation required",
                        For = "Manage Role Delete",
                        Type = "Error",
                        Data = new
                        {
                            UserIdentifier = identityHId
                        },
                        Tags = new string[]
                        {
                        ResponseTags.DO_MFA
                        }
                    }
                );
            }

            if (!_mfaService.ValidateMFARequest(identityHId, request.mfaCode))
            {
                return Ok(
                    new
                    {
                        Message = "2FA Validation failed",
                        For = "Manage Role Delete",
                        Type = "Error",
                        Data = new
                        {
                            UserIdentifier = identityHId
                        }
                    }
                );
            }

            if (!await _userService.HasRole(
                identity,
                r => r,
                role
                )
            )
            {
                return Ok(
                    new
                    {
                        Message = "You can't manage this role because you don't have it",
                        For = "Manage Role Delete",
                        Type = "Error",
                        Data = new
                        {
                            RoleIdentifier = roleIdentifier.roleId
                        }
                    }
                );
            }

            await _rolesService.DeleteRole(role);

            return Ok(
                new
                {
                    Message = "Role Deleted",
                    For = "Manage Role Delete",
                    Type = "Success",
                    Data = new
                    {
                        RoleIdentifier = roleIdentifier.roleId
                    },
                    JumpTo = Url.Action(
                        action: "ManageRoles",
                        controller: "Administrator",
                        values: new
                        {
                            Area = "Roles"
                        }
                    )
                }
            );
        }

        // 2fa if critical
        [HttpPost("/administrator/roles/{roleId}/editname")]
        [Permission(
            ServicePermissions.Roles_Administrator_Role,
            ServicePermissions.Roles_Administrator_Role_Name
        )]
        public async Task<IActionResult> ManageRoleName(
            [FromRoute] In_PartialRoleIdentifier roleIdentifier,
            [FromBody] In_ManageRoleName request
        )
        {
            if (request.Name == null)
            {
                return Ok(
                    new
                    {
                        Message = "Missing Data",
                        For = "Manage Role Name",
                        Type = "Error"
                    }
                );
            }

            Role? role = await _rolesService.GetRoleFromPartial(roleIdentifier.roleId);
            if (role == null)
            {
                return Ok(
                    new
                    {
                        Message = "Role not found",
                        For = "Manage Role Name",
                        Type = "Error",
                        Data = new
                        {
                            RoleIdentifier = roleIdentifier.roleId
                        }
                    }
                );
            }

            User? identity = await GetIdentity();
            if (identity == null)
            {
                return BadRequest();
            }

            if (role.IsCritical)
            {
                var identityHId = _userService.GetUserHashId(identity.Id);
                if (request.mfaCode == null)
                {
                    return Ok(
                        new
                        {
                            Message = "2FA Validation required",
                            For = "Manage Role Name",
                            Type = "Error",
                            Data = new
                            {
                                UserIdentifier = identityHId
                            },
                            Tags = new string[]
                            {
                            ResponseTags.DO_MFA
                            }
                        }
                    );
                }

                if (!_mfaService.ValidateMFARequest(identityHId, request.mfaCode))
                {
                    return Ok(
                        new
                        {
                            Message = "2FA Validation failed",
                            For = "Manage Role Name",
                            Type = "Error",
                            Data = new
                            {
                                UserIdentifier = identityHId
                            }
                        }
                    );
                }
            }

            if (!await _userService.HasRole(
                identity,
                r => r,
                role
                )
            )
            {
                return Ok(
                    new
                    {
                        Message = "You can't manage this role because you don't have it",
                        For = "Manage Role Name",
                        Type = "Error",
                        Data = new
                        {
                            RoleIdentifier = roleIdentifier.roleId
                        }
                    }
                );
            }

            role.Name = request.Name;

            await _rolesService.UpdateRole(role);

            return Ok(
                new
                {
                    Message = "Role Name Updated",
                    For = "Manage Role Name",
                    Type = "Success",
                    Data = new
                    {
                        RoleIdentifier = roleIdentifier.roleId
                    }
                }
            );
        }

        // allways critical
        [HttpPost("/administrator/roles/{roleId}/togglecritical")]
        [Permission(
            ServicePermissions.Roles_Administrator_Role,
            ServicePermissions.Roles_Administrator_Role_Critical
        )]
        public async Task<IActionResult> ManageRoleCritical(
            [FromRoute] In_PartialRoleIdentifier roleIdentifier,
            [FromBody] In_ManageRoleCritical request
        )
        {
            Role? role = await _rolesService.GetRoleFromPartial(roleIdentifier.roleId);
            if (role == null)
            {
                return Ok(
                    new
                    {
                        Message = "Role not found",
                        For = "Manage Role Critical",
                        Type = "Error"
                    }
                );
            }

            User? identity = await GetIdentity();
            if (identity == null)
            {
                return BadRequest();
            }

            var identityHId = _userService.GetUserHashId(identity.Id);
            if (request.mfaCode == null)
            {
                return Ok(
                    new
                    {
                        Message = "2FA Validation required",
                        For = "Manage Role Critical",
                        Type = "Error",
                        Data = new
                        {
                            UserIdentifier = identityHId
                        },
                        Tags = new string[]
                        {
                        ResponseTags.DO_MFA
                        }
                    }
                );
            }

            if (!_mfaService.ValidateMFARequest(identityHId, request.mfaCode))
            {
                return Ok(
                    new
                    {
                        Message = "2FA Validation failed",
                        For = "Manage Role Critical",
                        Type = "Error",
                        Data = new
                        {
                            UserIdentifier = identityHId
                        }
                    }
                );
            }

            if (!await _userService.HasRole(
                identity,
                r => r,
                role
                )
            )
            {
                return Ok(
                    new
                    {
                        Message = "You can't manage this role because you don't have it.",
                        For = "Manage Role Critical",
                        Type = "Error",
                        Data = new
                        {
                            RoleIdentifier = roleIdentifier.roleId
                        }
                    }
                );
            }

            role.IsCritical = !role.IsCritical;

            await _rolesService.UpdateRole(role);

            return Ok(
                new
                {
                    Message = "Role Critical Updated",
                    For = "Manage Role Critical",
                    Type = "Error",
                    Data = new
                    {
                        RoleIdentifier = roleIdentifier.roleId
                    }
                }
            );
        }

        // 2fa if critical
        [HttpPost("/administrator/roles/{roleId}/toggledefault")]
        [Permission(
            ServicePermissions.Roles_Administrator_Role,
            ServicePermissions.Roles_Administrator_Role_Default
        )]
        public async Task<IActionResult> ManageRoleDefault(
            [FromRoute] In_PartialRoleIdentifier roleIdentifier,
            [FromBody] In_ManageRoleDefault request
        )
        {
            Role? role = await _rolesService.GetRoleFromPartial(roleIdentifier.roleId);
            if (role == null)
            {
                return Ok(
                    new
                    {
                        Message = "Role not found",
                        For = "Manage Role Default",
                        Type = "Error"
                    }
                );
            }

            User? identity = await GetIdentity();
            if (identity == null)
            {
                return BadRequest();
            }

            if (role.IsCritical)
            {
                var identityHId = _userService.GetUserHashId(identity.Id);
                if (request.mfaCode == null)
                {
                    return Ok(
                        new
                        {
                            Message = "2FA Validation required",
                            For = "Manage Role Default",
                            Type = "Error",
                            Data = new
                            {
                                UserIdentifier = identityHId
                            },
                            Tags = new string[]
                            {
                            ResponseTags.DO_MFA
                            }
                        }
                    );
                }

                if (!_mfaService.ValidateMFARequest(identityHId, request.mfaCode))
                {
                    return Ok(
                        new
                        {
                            Message = "2FA Validation failed",
                            For = "Manage Role Default",
                            Type = "Error",
                            Data = new
                            {
                                UserIdentifier = identityHId
                            }
                        }
                    );
                }
            }

            if (!await _userService.HasRole(
                identity,
                r => r,
                role
                )
            )
            {
                return Ok(
                    new
                    {
                        Message = "You can't manage this role because you don't have it",
                        For = "Manage Role Default",
                        Type = "Error",
                        Data = new
                        {
                            RoleIdentifier = roleIdentifier.roleId
                        }
                    }
                );
            }

            role.IsDefault = !role.IsDefault;

            await _rolesService.UpdateRole(role);

            return Ok(
                new
                {
                    Message = "Role Default Updated",
                    For = "Manage Role Default",
                    Type = "Success",
                    Data = new
                    {
                        RoleIdentifier = roleIdentifier.roleId
                    }
                }
            );
        }


        [HttpGet("/administrator/roles/{roleId}/permissions")]
        [Permission(
            ServicePermissions.Roles_Administrator_Role,
            ServicePermissions.Roles_Administrator_Role_Permissions
        )]
        public async Task<IActionResult> ManageRolePermissions(
            [FromRoute] In_PartialRoleIdentifier roleIdentifier
        )
        {
            Role? role = await _rolesService.GetRoleFromPartial(roleIdentifier.roleId);
            if (role == null)
            {
                return Ok(
                    new
                    {
                        Message = "Role not found",
                        For = "Manage Role Permissions",
                        Type = "Error"
                    }
                );
            }

            return View(
                new Out_ManageRolePermissions
                {
                    RoleId = _rolesService.GetRoleHashId(role.Id),
                    Rolename = role.Name,
                    Permissions = (
                        from permission
                        in
                            await _rolesService.GetAllRolePermissions(role.Id).OrderBy(p => p.Name).ToListAsync()
                        select new _Permission
                        {
                            PermissionId = _permissionsService.GetPermissionHashId(permission.Id),
                            Name = permission.Name
                        }
                    )
                }
            );
        }

        // 2fa if critical
        [HttpPost("/administrator/roles/{roleId}/permissions/grant")]
        [Permission(
            ServicePermissions.Roles_Administrator_Role,
            ServicePermissions.Roles_Administrator_Role_Permissions,
            ServicePermissions.Roles_Administrator_Role_Permissions_Grant
        )]
        public async Task<IActionResult> ManageRolePermissionsGrant(
            [FromRoute] In_PartialRoleIdentifier roleIdentifier,
            [FromBody] In_ManageRolePermissionsGrant request
        )
        {
            Role? role = await _rolesService.GetRoleFromPartial(roleIdentifier.roleId);
            if (role == null)
            {
                return Ok(
                    new
                    {
                        Message = "Role not found",
                        For = "Manage Role Permissions Grant",
                        Type = "Error",
                        Data = new
                        {
                            RoleIdentifier = roleIdentifier.roleId
                        }
                    }
                );
            }

            Permission? permission = await _permissionsService.GetPermissionFromPartial("@" + request.PermissionName);
            if (permission == null)
            {
                return Ok(
                    new
                    {
                        Message = "Permission not found",
                        For = "Manage Role Permissions Grant",
                        Type = "Error",
                        Data = new
                        {
                            RoleIdentifier = roleIdentifier.roleId,
                            PermissionIdentifier = "@" + request.PermissionName
                        }
                    }
                );
            }

            User? identity = await GetIdentity();
            if (identity == null)
            {
                return BadRequest();
            }

            if (role.IsCritical || permission.IsCritical)
            {
                var identityHId = _userService.GetUserHashId(identity.Id);
                if (request.mfaCode == null)
                {
                    return Ok(
                        new
                        {
                            Message = "2FA Validation required",
                            For = "Manage Role Permissions Grant",
                            Type = "Error",
                            Data = new
                            {
                                UserIdentifier = identityHId
                            },
                            Tags = new string[]
                            {
                            ResponseTags.DO_MFA
                            }
                        }
                    );
                }

                if (!_mfaService.ValidateMFARequest(identityHId, request.mfaCode))
                {
                    return Ok(
                        new
                        {
                            Message = "2FA Validation failed",
                            For = "Manage Role Permissions Grant",
                            Type = "Error",
                            Data = new
                            {
                                UserIdentifier = identityHId
                            }
                        }
                    );
                }
            }

            
            if (!await _userService.HasRole(
                identity,
                r => r,
                role
            )
            )
            {
                return Ok(
                    new
                    {
                        Message = "You can't manage this role because you don't have it",
                        For = "Manage Role Permissions Grant",
                        Type = "Error",
                        Data = new
                        {
                            RoleIdentifier = roleIdentifier.roleId
                        }
                    }
                );
            }

            // skz!
            //if (!await _userService.HasPermission(
            //    identity,
            //    r => r,
            //    permission
            //    )
            //)
            //{
            //    return Ok(
            //        new
            //        {
            //            Message = "You can't give the role this permission because you don't have it",
            //            For = "Manage Role Permissions Grant",
            //            Type = "Error",
            //            Data = new
            //            {
            //                RoleIdentifier = roleIdentifier.roleId,
            //                PermissionIdentifier = "@" + request.PermissionName
            //            }
            //        }
            //    );
            //}

            await _rolesService.GrantPermission(role, permission);

            return Ok(
                new
                {
                    Message = "Role Permissions Updated",
                    For = "Manage Role Permissions Grant",
                    Type = "Success",
                    Data = new
                    {
                        RoleIdentifier = roleIdentifier.roleId,
                        PermissionIdentifier = "@" + request.PermissionName
                    }
                }
            );
        }

        // 2fa if critical
        [HttpPost("/administrator/roles/{roleId}/permissions/revoke")]
        [Permission(
            ServicePermissions.Roles_Administrator_Role,
            ServicePermissions.Roles_Administrator_Role_Permissions,
            ServicePermissions.Roles_Administrator_Role_Permissions_Revoke
        )]
        public async Task<IActionResult> ManageRolePermissionsRevoke(
            [FromRoute] In_PartialRoleIdentifier roleIdentifier,
            [FromBody] In_ManageRolePermissionsRevoke request
        )
        {
            if (request.PermissionId == null)
            {
                return Ok(
                    new
                    {
                        Message = "Missing Data",
                        For = "Manage Role Permissions Revoke",
                        Type = "Error"
                    }
                );
            }

            Role? role = await _rolesService.GetRoleFromPartial(roleIdentifier.roleId);
            if (role == null)
            {
                return Ok(
                    new
                    {
                        Message = "Role not found",
                        For = "Manage Role Permissions Revoke",
                        Type = "Error"
                    }
                );
            }

            Permission? permission = await _permissionsService.GetPermissionFromPartial(request.PermissionId);
            if (permission == null)
            {
                return Ok(
                    new
                    {
                        Message = "Permission not found",
                        For = "Manage Role Permissions Revoke",
                        Type = "Error"
                    }
                );
            }

            User? identity = await GetIdentity();
            if (identity == null)
            {
                return BadRequest();
            }

            if (role.IsCritical || permission.IsCritical)
            {
                var identityHId = _userService.GetUserHashId(identity.Id);
                if (request.mfaCode == null)
                {
                    return Ok(
                        new
                        {
                            Message = "2FA Validation required",
                            For = "Manage Role Permissions Revoke",
                            Type = "Error",
                            Data = new
                            {
                                UserIdentifier = identityHId
                            },
                            Tags = new string[]
                            {
                            ResponseTags.DO_MFA
                            }
                        }
                    );
                }

                if (!_mfaService.ValidateMFARequest(identityHId, request.mfaCode))
                {
                    return Ok(
                        new
                        {
                            Message = "2FA Validation failed",
                            For = "Manage Role Permissions Revoke",
                            Type = "Error",
                            Data = new
                            {
                                UserIdentifier = identityHId
                            }
                        }
                    );
                }
            }

            if (!await _userService.HasRole(
                identity,
                r => r,
                role
                )
            )
            {
                return Ok(
                    new
                    {
                        Message = "You can't manage this role because you don't have it",
                        For = "Manage Role Permissions Revoke",
                        Type = "Error",
                        Data = new
                        {
                            RoleIdentifier = roleIdentifier.roleId,
                            PermissionIdentifier = request.PermissionId
                        }
                    }
                );
            }
            
            // skz!
            //if (!await _userService.HasPermission(
            //    identity,
            //    r => r,
            //    permission
            //    )
            //)
            //{
            //    return Ok(
            //        new
            //        {
            //            Message = "You can't revoke this permission from the role because you don't have it",
            //            For = "Manage Role Permissions Revoke",
            //            Type = "Error",
            //            Data = new
            //            {
            //                RoleIdentifier = roleIdentifier.roleId,
            //                PermissionIdentifier = request.PermissionId
            //            }
            //        }
            //    );
            //}

            //await _rolesService.RevokePermission(role, permission);

            return
            Ok(
                new
                {
                    Message = "Role Permissions Updated",
                    For = "Manage Role Permissions Revoke",
                    Type = "Success",
                    Data = new
                    {
                        RoleIdentifier = roleIdentifier.roleId,
                        PermissionIdentifier = request.PermissionId
                    }
                }
            );
        }
    }
}
