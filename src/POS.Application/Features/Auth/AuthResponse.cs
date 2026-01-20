
// POS.Application/Features/Auth/AuthResponse.cs
using System.Collections.Generic;
using POS.Application.Features.Role;

namespace POS.Application.Features.Auth
{
    public class AuthResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public List<string> Permissions { get; set; } = new();
        public List<RoleInfos> Roles { get; set; } = new();
    }
}
