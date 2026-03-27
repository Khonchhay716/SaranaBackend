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

            new StaticPermission { Name = "role:list", Description = "list role", Category = "Roles" },
            new StaticPermission { Name = "role:create", Description = "Create role", Category = "Roles" },
            new StaticPermission { Name = "role:read", Description = "Read role details", Category = "Roles" },
            new StaticPermission { Name = "role:update", Description = "Update role", Category = "Roles" },
            new StaticPermission { Name = "role:delete", Description = "Delete role", Category = "Roles" },
            new StaticPermission { Name = "role:assign-permissions", Description = "Assign permissions to role", Category = "Roles" },

            new StaticPermission { Name = "user:lookup", Description = "lookup person", Category = "Persons" },
            new StaticPermission { Name = "user:list", Description = "list person", Category = "Persons" },
            new StaticPermission { Name = "user:create", Description = "Create person", Category = "Persons" },
            new StaticPermission { Name = "user:read", Description = "Read person details", Category = "Persons" },
            new StaticPermission { Name = "user:update", Description = "Update person", Category = "Persons" },
            new StaticPermission { Name = "user:delete", Description = "Delete person", Category = "Persons" },
            new StaticPermission { Name = "user:assign-roles", Description = "Assign roles to person", Category = "Persons" },

            /// feature 
            new StaticPermission { Name = "category:create", Description = "Create category", Category = "Category" },
            new StaticPermission { Name = "category:read", Description = "List book category", Category = "Category" },
            new StaticPermission { Name = "category:update", Description = "Update category", Category = "Category" },
            new StaticPermission { Name = "category:delete", Description = "Delete category", Category = "Category" },
            new StaticPermission { Name = "category:view", Description = "View category detail", Category = "Category" },

            new StaticPermission { Name = "product:create", Description = "Create product", Category = "Products" },
            new StaticPermission { Name = "product:read", Description = "List product", Category = "Products" },
            new StaticPermission { Name = "product:update", Description = "Update product", Category = "Products" },
            new StaticPermission { Name = "product:delete", Description = "Delete product", Category = "Products" },
            new StaticPermission { Name = "product:view", Description = "View product detail", Category = "Products" },

        };
    }
}