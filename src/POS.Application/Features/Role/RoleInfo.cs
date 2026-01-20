using System.Collections.Generic;

namespace POS.Application.Features.Role
{
    public class RoleInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Permissions { get; set; } = new();
    }

    public class RoleInfos
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }


    public class RolePermissionResponse
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = default!;
        public string RoleDescription { get; set; } = default!;
        public List<string> AssignedPermissions { get; set; } = new();
        public List<PermissionGroupDto> AllPermissions { get; set; } = new();
    }

    public class PermissionGroupDto
    {
        public string Group { get; set; } = default!;
        public List<PermissionItemDto> Permissions { get; set; } = new();
    }

    public class PermissionItemDto
    {
        public string Name { get; set; } = default!;
        public string Description { get; set; } = default!;
        public bool IsAssigned { get; set; }
    }
}