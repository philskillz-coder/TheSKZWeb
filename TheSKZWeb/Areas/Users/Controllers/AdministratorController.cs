
using Microsoft.AspNetCore.Mvc;
using TheSKZWeb.AuthorizationPolicies;
using TheSKZWeb.Models;
using TheSKZWeb.Overwrites;
using TheSKZWeb.Services;

using TheSKZWeb.Areas.Users.Models.Administrator.In;
using TheSKZWeb.Areas.Users.Models.Administrator.Out;
using Microsoft.EntityFrameworkCore;

namespace TheSKZWeb.Areas.Users.Controllers
{
    [Area("Users")]
    [Permission(
        PagePermissions.Users,
        PagePermissions.Users_Administrator
    )]
    public class AdministratorController : NewController
    {
        private readonly IPermissionsService _permissionsService;
        private readonly IRolesService _rolesService;
        private readonly IMFAService _mfaService;

        public AdministratorController(IUserService userService, IPermissionsService permissionsService, IRolesService roleService,
            IMFAService mfaService) : base(userService)
        {
            _permissionsService = permissionsService;
            _rolesService = roleService;
            _mfaService = mfaService;
        }

        [HttpGet("/administrator/users")]
        public async Task<IActionResult> ManageUsers(
            [FromQuery] In_ManageUsers request
        )
        {
            User? highlightedUser = await _userService.GetUserByPartial(request.Highlighted);

            List<_User> mUsers = new List<_User>();
            IEnumerable<User> users = await _userService.GetAllUsers().ToArrayAsync();

            foreach (var user in users)
            {
                mUsers.Add(new _User
                {
                    UserId = _userService.GetUserHashId(user.Id),
                    Username = user.Username,
                    Highlighted = user.Id == highlightedUser?.Id,
                    MFAEnabled = user.MFAEnabled
                });
            }

            return View(
                new Out_ManageUsers
                {
                    Users = mUsers.OrderBy(u => u.Username),
                    Highlighted = highlightedUser != null ? new _User
                    {
                        UserId = _userService.GetUserHashId(highlightedUser.Id),
                        Username = highlightedUser.Username,
                        Highlighted = true
                    } : null
                }
            );
        }


        [HttpGet("/administrator/users/{userId}")]
        [Permission(
            PagePermissions.Users_Administrator_User
        )]
        public async Task<IActionResult> ManageUser(
            [FromRoute] In_PartialUserIdentifier userIdentifier
        )
        {
            User? user = await _userService.GetUserByPartial(userIdentifier.userId);
            if (user == null)
            {
                return Ok(
                    new
                    {
                        Message = "User not found",
                        For = "Manage User",
                        Type = "Error",
                        Data = new
                        {
                            UserIdentifier = userIdentifier.userId
                        }
                    }
                );
            }

            return View(
                new Out_ManageUser
                {
                    UserId = _userService.GetUserHashId(user.Id),
                    Username = user.Username,
                    MFAEnabled = user.MFAEnabled,
                    CreatedAt = user.CreatedAt,
                    LastModifiedAt = user.LastModifiedAt
                }
            );
        }

        [HttpPost("/administrator/users/{userId}/editusername")]
        [Permission(
            PagePermissions.Users_Administrator_User,
            PagePermissions.Users_Administrator_User_Name
        )]
        public async Task<IActionResult> ManageUserUsername(
            [FromRoute] In_PartialUserIdentifier userIdentifier,
            [FromBody] In_ManageUserUsername request
        )
        {
            if (request.Username == null)
            {
                return Ok(
                    new
                    {
                        Message = "Missing Data!",
                        For = "Manage User Username",
                        Type = "Error"
                    }
                );
            }

            User? user = await _userService.GetUserByPartial(userIdentifier.userId);
            if (user == null)
            {
                return Ok(
                    new
                    {
                        Message = "User not found",
                        For = "Manage User Username",
                        Type = "Error",
                        Data = new
                        {
                            UserIdentifier = userIdentifier.userId
                        }
                    }
                );
            }

            if (await _userService.UsernameExists(request.Username))
            {
                return Ok(
                    new
                    {
                        Message = "Username already exists",
                        For = "Manage User Username",
                        Type = "Error",
                        Data = new
                        {
                            UserIdentifier = userIdentifier.userId,
                            Username = request.Username
                        }
                    }
                );
            }

            user.Username = request.Username;
            await _userService.UpdateUser(user);

            return Ok(
                new
                {
                    Message = "User Username Updated",
                    For = "Manage User Username",
                    Type = "Success",
                    Data = new
                    {
                        UserIdentifier = userIdentifier.userId
                    }
                }
            );
        }

        // allways 2fa
        [HttpPost("/administrator/users/{userId}/deleteuser")]
        [Permission(
            PagePermissions.Users_Administrator_User,
            PagePermissions.Users_Administrator_User_Delete
        )]
        public async Task<IActionResult> ManageUserDelete(
            [FromRoute] In_PartialUserIdentifier userIdentifier,
            [FromBody] In_ManageUserDelete request
        )
        {
            User? user = await _userService.GetUserByPartial(userIdentifier.userId);
            if (user == null)
            {
                return Ok(
                    new
                    {
                        Message = "User not found",
                        For = "Manage User Delete",
                        Type = "Error",
                        Data = new
                        {
                            UserIdentifier = userIdentifier.userId
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
                        For = "Manage User Delete",
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
                        For = "Manage User Delete",
                        Type = "Error",
                        Data = new
                        {
                            UserIdentifier = identityHId
                        }
                    }
                );
            }

            await _userService.DeleteUser(user);

            return Ok(
                new
                {
                    Message = "User Deleted",
                    For = "Manage User Delete",
                    Type = "Success",
                    Data = new
                    {
                        UserIdentifier = userIdentifier.userId
                    },
                    JumpTo = Url.Action(
                        action: "ManageUsers",
                        controller: "Administrator",
                        values: new
                        {
                            Area = "Users"
                        }
                    )
                }
            );
        }




        [HttpGet("/administrator/users/{userId}/permissions")]
        [Permission(
            PagePermissions.Users_Administrator_User,
            PagePermissions.Users_Administrator_User_Permissions
        )]
        public async Task<IActionResult> ManageUserPermissions(
            [FromRoute] In_PartialUserIdentifier userIdentifier
        )
        {
            User? user = await _userService.GetUserByPartial(userIdentifier.userId);
            if (user == null)
            {
                return Ok(
                    new
                    {
                        Message = "User not found",
                        For = "Manage User Permissions",
                        Type = "Error",
                        Data = new
                        {
                            UserIdentifier = userIdentifier.userId
                        }
                    }
                );
            }

            return View(
                new Out_ManageUserPermissions
                {
                    UserId = _userService.GetUserHashId(user.Id),
                    Username = user.Username,
                    Permissions = (
                        from permission
                        in
                            await _userService.GetUserPermissions(user.Id).OrderBy(p => p.Name).ToListAsync()
                        select new _Permission
                        {
                            PermissionId = _permissionsService.GetPermissionHashId(permission.Id),
                            Name = permission.Name
                        }
                    )
                }
            );
        }


        [HttpPost("/administrator/users/{userId}/permissions/grant")]
        [Permission(
            PagePermissions.Users_Administrator_User,
            PagePermissions.Users_Administrator_User_Permissions,
            PagePermissions.Users_Administrator_User_Permissions_Grant
        )]
        public async Task<IActionResult> ManageUserPermissionsGrant(
            [FromRoute] In_PartialUserIdentifier userIdentifier,
            [FromBody] In_ManageUserPermissionsGrant request
        )
        {
            if (request.PermissionName == null)
            {
                return Ok(
                    new
                    {
                        Message = "Missing Data",
                        For = "Manage User Permissions Grant",
                        Type = "Error"
                    }
                );
            }

            User? user = await _userService.GetUserByPartial(userIdentifier.userId);
            if (user == null)
            {
                return Ok(
                    new
                    {
                        Message = "User not found",
                        For = "Manage User Permissions Grant",
                        Type = "Error",
                        Data = new
                        {
                            UserIdentifier = userIdentifier.userId
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
                        For = "Manage User Permissions Grant",
                        Type = "Error",
                        Data = new
                        {
                            UserIdentifier = userIdentifier.userId,
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

            if (!await _userService.HasPermission(
                identity,
                p => p,
                permission
                )
            )
            {
                return Ok(
                    new
                    {
                        Message = "You can't grant this permission to the user because you don't have it",
                        For = "Manage User Permissions Grant",
                        Type = "Error",
                        Data = new
                        {
                            UserIdentifier = userIdentifier.userId,
                            PermissionIdentifier = "@" + request.PermissionName
                        }
                    }
                );
            }

            await _userService.GrantPermission(user, permission);

            return Ok(
                new
                {
                    Message = "User Permission Granted",
                    For = "Manage User Permissions Grant",
                    Type = "Success",
                    Data = new
                    {
                        UserIdentifier = userIdentifier.userId,
                        PermissionIdentifier = "@" + request.PermissionName
                    }
                }
            );
        }

        [HttpPost("/administrator/users/{userId}/permissions/revoke")]
        [Permission(
            PagePermissions.Users_Administrator_User,
            PagePermissions.Users_Administrator_User_Permissions,
            PagePermissions.Users_Administrator_User_Permissions_Revoke
        )]
        public async Task<IActionResult> ManageUserPermissionsRevoke(
            [FromRoute] In_PartialUserIdentifier userIdentifier,
            [FromBody] In_ManageUserPermissionsRevoke request
        )
        {

            if (request.PermissionId == null)
            {
                return Ok(
                    new
                    {
                        Message = "Missing Data",
                        For = "Manage User Permissions Revoke",
                        Type = "Error"
                    }
                );
            }

            User? user = await _userService.GetUserByPartial(userIdentifier.userId);
            if (user == null)
            {
                return Ok(
                    new
                    {
                        Message = "User not found",
                        For = "Manage User Permissions Revoke",
                        Type = "Error",
                        Data = new
                        {
                            UserIdentifier = userIdentifier.userId
                        }
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
                        For = "Manage User Permissions Revoke",
                        Type = "Error",
                        Data = new
                        {
                            UserIdentifier = userIdentifier.userId,
                            PermissionIdentifier = request.PermissionId
                        }
                    }
                );
            }

            User? identity = await GetIdentity();
            if (identity == null)
            {
                return BadRequest();
            }

            if (!await _userService.HasPermission(
                identity,
                p => p,
                permission
                )
            )
            {
                return Ok(
                    new
                    {
                        Message = "You can't revoke this permission from the user because you don't have it",
                        For = "Manage User Permissions Revoke",
                        Type = "Error",
                        Data = new
                        {
                            UserIdentifier = userIdentifier.userId,
                            PermissionIdentifier = request.PermissionId
                        }
                    }
                );
            }

            await _userService.RevokePermission(user, permission);

            return Ok(
                new
                {
                    Message = "User Permission Revoked",
                    For = "Manage User Permissions Revoke",
                    Type = "Success",
                    Data = new
                    {
                        UserIdentifier = userIdentifier.userId
                    }
                }
            );
        }



        [HttpGet("/administrator/users/{userId}/roles")]
        [Permission(
            PagePermissions.Users_Administrator_User,
            PagePermissions.Users_Administrator_User_Roles
        )]
        public async Task<IActionResult> ManageUserRoles(
            [FromRoute] In_PartialUserIdentifier userIdentifier
        )
        {
            User? user = await _userService.GetUserByPartial(userIdentifier.userId);
            if (user == null)
            {
                return Ok(
                    new
                    {
                        Message = "User not found",
                        For = "Manage User Roles",
                        Type = "Error",
                        Data = new
                        {
                            UserIdentifier = userIdentifier.userId
                        }
                    }
                );
            }

            return View(
                new Out_ManageUserRoles
                {
                    UserId = _userService.GetUserHashId(user.Id),
                    Username = user.Username,
                    Roles = (
                        from role
                        in
                            await _userService.GetUserRoles(user.Id).ToListAsync()
                        select new _Role
                        {
                            RoleId = _rolesService.GetRoleHashId(role.Id),
                            Name = role.Name
                        }
                    )
                }
            );
        }

        [HttpPost("/administrator/users/{userId}/roles/grant")]
        [Permission(
            PagePermissions.Users_Administrator_User,
            PagePermissions.Users_Administrator_User_Roles,
            PagePermissions.Users_Administrator_User_Roles_Grant
        )]
        public async Task<IActionResult> ManageUserRolesGrant(
            [FromRoute] In_PartialUserIdentifier userIdentifier,
            [FromBody] In_ManageUserRolesGrant request
        )
        {
            if (request.RoleName == null)
            {
                return Ok(
                    new
                    {
                        Message = "Missing Data",
                        For = "Manage User Roles Grant",
                        Type = "Error"
                    }
                );
            }

            User? user = await _userService.GetUserByPartial(userIdentifier.userId);
            if (user == null)
            {
                return Ok(
                    new
                    {
                        Message = "User not found",
                        For = "Manage User Roles Grant",
                        Type = "Error",
                        Data = new
                        {
                            UserIdentifier = userIdentifier.userId
                        }
                    }
                );
            }

            Role? role = await _rolesService.GetRoleByName(request.RoleName);
            if (role == null)
            {
                return Ok(
                    new
                    {
                        Message = "Role not found",
                        For = "Manage User Roles Grant",
                        Type = "Error",
                        Data = new
                        {
                            UserIdentifier = userIdentifier.userId,
                            RoleIdentifier = "@" + request.RoleName
                        }
                    }
                );
            }

            User? identity = await GetIdentity();
            if (identity == null)
            {
                return BadRequest();
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
                        Message = "You can't give this role to the user because you don't have it",
                        For = "Manage User Roles Grant",
                        Type = "Error",
                        Data = new
                        {
                            UserIdentifier = userIdentifier.userId,
                            RoleIdentifier = "@" + request.RoleName
                        }
                    }
                );
            }

            await _userService.GrantRole(user, role);

            return Ok(
                new
                {
                    Message = "User role granted",
                    For = "Manage User Roles Grant",
                    Type = "Success",
                    Data = new
                    {
                        UserIdentifier = userIdentifier.userId
                    }
                }
            );
        }

        [HttpPost("/administrator/users/{userId}/roles/revoke")]
        [Permission(
            PagePermissions.Users_Administrator_User,
            PagePermissions.Users_Administrator_User_Roles,
            PagePermissions.Users_Administrator_User_Roles_Revoke
        )]
        public async Task<IActionResult> ManageUserRolesRevoke(
            [FromRoute] In_PartialUserIdentifier userIdentifier,
            [FromBody] In_ManageUserRolesRevoke request
        )
        {

            if (request.RoleId == null)
            {
                return Ok(
                    new
                    {
                        Message = "Missing Data",
                        For = "Manage User Roles Revoke",
                        Type = "Error"
                    }
                );
            }

            User? user = await _userService.GetUserByPartial(userIdentifier.userId);
            if (user == null)
            {
                return Ok(
                    new
                    {
                        Message = "User not found",
                        For = "Manage User Roles Revoke",
                        Type = "Error",
                        Data = new
                        {
                            UserIdentifier = userIdentifier.userId
                        }
                    }
                );
            }

            Role? role = await _rolesService.GetRoleFromPartial(request.RoleId);
            if (role == null)
            {
                return Ok(
                    new
                    {
                        Message = "Role not found",
                        For = "Manage User Roles Revoke",
                        Type = "Error",
                        Data = new
                        {
                            UserIdentifier = userIdentifier.userId,
                            RoleIdentifier = request.RoleId
                        }
                    }
                );
            }

            User? identity = await GetIdentity();
            if (identity == null)
            {
                return BadRequest();
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
                        Message = "You can't revoke this role from the user because you don't have it",
                        For = "Manage User Roles Revoke",
                        Type = "Error",
                        Data = new
                        {
                            UserIdentifier = userIdentifier.userId,
                            RoleIdentifier = request.RoleId
                        }
                    }
                );
            }

            await _userService.RevokeRole(user, role);

            return Ok(
                new
                {
                    Message = "User role revoked",
                    For = "Manage User Roles Revoke",
                    Type = "Success",
                    Data = new
                    {
                        UserIdentifIier = userIdentifier.userId
                    }
                }
            );
        }
    }
}
