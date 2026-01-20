// using MediatR;
// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Mvc;
// using POS.API.Attributes;
// using POS.Application.Features.Permission;
// using POS.Application.Features.Role;
// using System.Collections.Generic;
// using System.Threading.Tasks;

// namespace POS.API.Controllers
// {
//     [ApiController]
//     [Route("api/[controller]")]
//     public class PermissionsController : ControllerBase
//     {
//         private readonly IMediator _mediator;

//         public PermissionsController(IMediator mediator)
//         {
//             _mediator = mediator;
//         }

//         [HttpGet]
//         // [RequirePermission("Permissions.View")]
//         [AllowAnonymous]
//         public async Task<ActionResult<List<PermissionInfo>>> GetAllPermissions()
//         {
//             var result = await _mediator.Send(new GetAllPermissionsQuery());
//             return Ok(result);
//         }

//         [HttpGet("role/{roleId}")]
//         // [RequirePermission("Roles.View")]
//         [AllowAnonymous]
//         public async Task<IActionResult> GetRolePermissions(int roleId)
//         {
//             var query = new GetRolePermissionsQuery(roleId);
//             var result = await _mediator.Send(query);
            
//             if (result == null)
//                 return NotFound($"Role with ID {roleId} not found");
                
//             return Ok(result);
//         }
//     }
// }



using MediatR;
using Microsoft.AspNetCore.Mvc;
using POS.API.Attributes;
using POS.Application.Features.Permission;
using POS.Application.Features.Role;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace POS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PermissionsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PermissionsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [RequirePermission("permission:read")]  // ⭐ Use proper permission check
        public async Task<ActionResult<List<PermissionInfo>>> GetAllPermissions()
        {
            var result = await _mediator.Send(new GetAllPermissionsQuery());
            return Ok(result);
        }

        [HttpGet("role/{roleId}")]
        [RequirePermission("permission:read")]  // ⭐ Use proper permission check
        public async Task<IActionResult> GetRolePermissions(int roleId)
        {
            var query = new GetRolePermissionsQuery(roleId);
            var result = await _mediator.Send(query);
            
            if (result == null)
                return NotFound($"Role with ID {roleId} not found");
                
            return Ok(result);
        }
    }
}