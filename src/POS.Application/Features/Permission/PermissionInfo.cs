namespace POS.Application.Features.Permission
{
    public class PermissionInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string Description { get; set; } = default!;
        public string Category { get; set; } = default!;
    }

    public class StaticPermission
    {
        public string Name { get; set; } = default!;
        public string Description { get; set; } = default!;
        public string Category { get; set; } = "Other";
    }

    public static class PermissionData
    {
        public static readonly List<StaticPermission> Permissions = new()
        {
            new StaticPermission { Name = "permission:read", Description = "permission", Category = "Permission" },

            new StaticPermission { Name = "coupon:create", Description = "Create coupon", Category = "Coupon" },
            new StaticPermission { Name = "coupon:delete", Description = "Delete coupon", Category = "Coupon" },
            new StaticPermission { Name = "coupon:list", Description = "View paginated list of coupons", Category = "Coupon" },
            new StaticPermission { Name = "coupon:read", Description = "Read coupon details", Category = "Coupon" },
            new StaticPermission { Name = "coupon:update", Description = "Update coupon", Category = "Coupon" },

            new StaticPermission { Name = "role:create", Description = "Create role", Category = "Roles" },
            new StaticPermission { Name = "role:read", Description = "Read role details", Category = "Roles" },
            new StaticPermission { Name = "role:update", Description = "Update role", Category = "Roles" },
            new StaticPermission { Name = "role:delete", Description = "Delete role", Category = "Roles" },
            new StaticPermission { Name = "role:assign-permissions", Description = "Assign permissions to role", Category = "Roles" },

            new StaticPermission { Name = "user:create", Description = "Create person", Category = "Persons" },
            new StaticPermission { Name = "user:read", Description = "Read person details", Category = "Persons" },
            new StaticPermission { Name = "user:update", Description = "Update person", Category = "Persons" },
            new StaticPermission { Name = "user:delete", Description = "Delete person", Category = "Persons" },
            new StaticPermission { Name = "user:assign-roles", Description = "Assign roles to person", Category = "Persons" },
        };
    }
}