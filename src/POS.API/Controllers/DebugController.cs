// POS.API/Controllers/DebugController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Application.Common.Interfaces;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace POS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DebugController : ControllerBase
    {
        private readonly ICurrentUserService _currentUserService;

        public DebugController(ICurrentUserService currentUserService)
        {
            _currentUserService = currentUserService;
        }

        [HttpGet("test-auth")]
        [Authorize]
        public IActionResult TestAuth()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
            
            var allClaims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();

            return Ok(new
            {
                IsAuthenticated = isAuthenticated,
                UserId = userId,
                Username = username,
                AllClaims = allClaims
            });
        }

        [HttpGet("test-permissions")]
        [Authorize]
        public async Task<IActionResult> TestPermissions()
        {
            var userId = _currentUserService.UserId;
            var permissions = await _currentUserService.GetPermissionsAsync();

            return Ok(new
            {
                UserId = userId,
                Permissions = permissions,
                PermissionCount = permissions.Count()
            });
        }

        [HttpGet("test-anonymous")]
        [AllowAnonymous]
        public IActionResult TestAnonymous()
        {
            return Ok(new { Message = "This endpoint allows anonymous access" });
        }
    }
}