using Microsoft.AspNetCore.Mvc;
using TheSKZWeb.AuthorizationPolicies;
using TheSKZWeb.Overwrites;
using TheSKZWeb.Services;

using TheSKZWeb.Areas.Permissions.Models.Administrator.In;
using TheSKZWeb.Areas.Permissions.Models.Administrator.Out;
using TheSKZWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace TheSKZWeb.Areas.Permissions.Controllers
{
    [Area("Permissions")]
    [Permission(
        PagePermissions.Permissions,
        PagePermissions.Permissions_Administrator
    )]
    public class AdministratorController : NewController
    {
        private readonly IPermissionsService _permissionsService;
        private readonly IMFAService _mfaService;

        public AdministratorController(IPermissionsService permissionsService, IUserService userService, IMFAService mfaService) : base(userService)
        {
            _permissionsService = permissionsService;
            _mfaService = mfaService;
        }

        [HttpGet("/administrator/permissions")]
        public async Task<IActionResult> ManagePermissions(
            [FromQuery] In_ManagePermissions request
        )
        {
            Permission? highlightedPermission = await _permissionsService.GetPermissionFromPartial(request.Highlighted);

            IEnumerable<_Permission> permissions = (
                from permission
                in (await _permissionsService.GetAllPermissions().OrderBy(p => p.Name).ToArrayAsync())
                select new _Permission
                {
                    PermissionId = _permissionsService.GetPermissionHashId(permission.Id),
                    IsDefault = permission.IsDefault,
                    Name = permission.Name,
                    Highlighted = permission.Id == highlightedPermission?.Id,
                }
            );

            return View(new Out_ManagePermissions
            {
                Permissions = permissions,
                Highlighted = highlightedPermission != null ? new _Permission
                {
                    PermissionId = _permissionsService.GetPermissionHashId(highlightedPermission.Id),
                    IsDefault = highlightedPermission.IsDefault,
                    Name = highlightedPermission.Name,
                    Highlighted = true
                } : null
            });
        }

        [HttpGet("/administrator/permissions/create")]
        [Permission(
            PagePermissions.Permissions_Administrator_Create
        )]
        public IActionResult ManagePermissionsCreate()
        {
            return View();
        }

        [HttpPost("/administrator/permissions/create")]
        [Permission(
            PagePermissions.Permissions_Administrator_Create
        )]
        public async Task<IActionResult> ManagePermissionsCreate(
            [FromBody] In_ManagePermissionCreate request
        )
        {
            if (request.Name == null || request.Default == null)
            {
                return Ok(
                    new
                    {
                        Message = "Missing Data!",
                        For = "Create Permission",
                        Type = "Error"
                    }
                );
            }

            if (await _permissionsService.PermissionNameExists(request.Name))
            {
                return Ok(
                    new
                    {
                        Message = "A permission with this name already exists.",
                        For = "Create Permission",
                        Type = "Error"
                    }
                );
            }


            Permission createdPermission = await _permissionsService.CreatePermission(request.Name, request.Default ?? false);


            User? identity = await GetIdentity();
            if (identity == null )
            {
                return BadRequest();
            }

            await _userService.GrantPermission(
                identity,
                createdPermission
            );


            return Ok(
                new
                {
                    Message = "Created Permission.",
                    For = "Create Permission",
                    Type = "Success",
                    Data = new
                    {
                        PermissionId = _permissionsService.GetPermissionHashId(createdPermission.Id),
                        PermissionName = createdPermission.Name
                    },
                    JumpTo = Url.Action(
                        action: "ManagePermissions",
                        controller: "Administrator",
                        values: new
                        {
                            Area = "Permissions",
                            Highlighted = _permissionsService.GetPermissionHashId(createdPermission.Id)
                        }
                    )
                }
            );
        }

        [HttpGet("/administrator/permissions/{permissionId}")]
        [Permission(
            PagePermissions.Permissions_Administrator_Permission
        )]
        public async Task<IActionResult> ManagePermission(
            [FromRoute] In_PartialPermissionIdentifier permissionIdentifier
        )
        {
            Permission? permission = await _permissionsService.GetPermissionFromPartial(permissionIdentifier.permissionId);
            if (permission == null)
            {
                return Ok(
                    new
                    {
                        Message = "Permission not found",
                        For = "Manage Permission",
                        Type = "Error",
                        Data = new
                        {
                            PermissionIdentifier = permissionIdentifier.permissionId
                        }
                    }
                );
            }

            return View(new Out_ManagePermission
            {
                PermissionId = _permissionsService.GetPermissionHashId(permission.Id),
                IsDefault = permission.IsDefault,
                Name = permission.Name,
                CreatedAt = permission.CreatedAt,
                LastModifiedAt = permission.LastModifiedAt
            });
        }



        // add 2fa
        [HttpPost("/administrator/permissions/{permissionId}/delete")]
        [Permission(
            PagePermissions.Permissions_Administrator_Permission,
            PagePermissions.Permissions_Administrator_Permission_Delete
        )]
        public async Task<IActionResult> ManagePermissionDelete(
            [FromRoute] In_PartialPermissionIdentifier permissionIdentifier,
            [FromBody] In_ManagePermissionDelete request
        )
        {
            Permission? permission = await _permissionsService.GetPermissionFromPartial(permissionIdentifier.permissionId);
            if (permission == null)
            {
                return Ok(
                    new
                    {
                        Message = "Permission not found",
                        For = "Manage Permission Delete",
                        Type = "Error",
                        Data = new
                        {
                            PermissionIdentifier = permissionIdentifier.permissionId
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
                        For = "Manage Permission Delete",
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
                        For = "Manage Permission Delete",
                        Type = "Error",
                        Data = new
                        {
                            UserIdentifier = identityHId
                        }
                    }
                );
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
                        Message = "You can't manage this permission because you don't have it.",
                        For = "Manage Permission",
                        Type = "Error",
                        Data = new
                        {
                            PermissionIdentifier = permissionIdentifier.permissionId
                        }
                    }
                );
            }

            await _permissionsService.DeletePermission(permission);

            return Ok(
            new
            {
                Message = "Permission Deleted!",
                For = "Manage Permission",
                Type = "Success",
                Data = new
                {
                    PermissionIdentifier = permissionIdentifier.permissionId
                },
                JumpTo = "/administrator/permissions"
            }
        );
        }


        

        [HttpPost("/administrator/permissions/{permissionId}/editname")]
        [Permission(
            PagePermissions.Permissions_Administrator_Permission,
            PagePermissions.Permissions_Administrator_Permission_Name
        )]
        public async Task<IActionResult> ManagePermissionName(
            [FromRoute] In_PartialPermissionIdentifier permissionIdentifier,
            [FromBody] In_ManagePermissionName request
        )
        {
            if (request.Name == null)
            {
                return Ok(
                    new
                    {
                        Message = "Missing Data!",
                        For = "Manage Permission Name",
                        Type = "Error",
                        Data = new
                        {
                            PermissionIdentifier = permissionIdentifier.permissionId
                        }
                    }
                );
            }

            Permission? permission = await _permissionsService.GetPermissionFromPartial(permissionIdentifier.permissionId);
            if (permission == null)
            {
                return Ok(
                    new
                    {
                        Message = "Permission not found",
                        For = "Manage Permission Name",
                        Type = "Error",
                        Data = new
                        {
                            PermissionIdentifier = permissionIdentifier.permissionId
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
                        Message = "You can't manage this permission because you don't have it",
                        For = "Manage Permission Name",
                        Type = "Error",
                        Data = new
                        {
                            PermissionIdentifier = permissionIdentifier.permissionId
                        }
                    }
                );
            }

            permission.Name = request.Name;

            await _permissionsService.UpdatePermission(permission);

            return Ok(
                new
                {
                    Message = "Permission Name Updated",
                    For = "Manage Permission Name",
                    Type = "Success",
                    Data = new
                    {
                        PermissionIdentifier = permissionIdentifier.permissionId
                    }
                }
            );
        }


        [HttpPost("/administrator/permissions/{permissionId}/toggledefault")]
        [Permission(
            PagePermissions.Permissions_Administrator_Permission,
            PagePermissions.Permissions_Administrator_Permission_Default
        )]
        public async Task<IActionResult> ManagePermissionDefault(
            [FromRoute] In_PartialPermissionIdentifier permissionIdentifier,
            [FromBody] In_ManagePermissionDefault request
        )
        {
            Permission? permission = await _permissionsService.GetPermissionFromPartial(permissionIdentifier.permissionId);
            if (permission == null)
            {
                return Ok(
                    new
                    {
                        Message = "Permission not found",
                        For = "Manage Permission Default",
                        Type = "Error",
                        Data = new
                        {
                            PermissionIdentifier = permissionIdentifier.permissionId
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
                        Message = "You can't manage this permission because you don't have it",
                        For = "Manage Permission Default",
                        Type = "Error",
                        Data = new
                        {
                            PermissionIdentifier = permissionIdentifier.permissionId
                        }
                    }
                );
            }

            permission.IsDefault = !permission.IsDefault;

            await _permissionsService.UpdatePermission(permission);

            return Ok(
                new
                {
                    Message = "Updated permission default",
                    For = "Manage Permission Default",
                    Type = "Success",
                    Data = new
                    {
                        PermissionIdentifier = permissionIdentifier.permissionId
                    }
                }
            );
        }
    }
}
