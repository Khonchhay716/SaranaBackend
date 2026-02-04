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

            new StaticPermission { Name = "book:lookup", Description = "Lookup book", Category = "Books" },
            new StaticPermission { Name = "book:create", Description = "Create book", Category = "Books" },
            new StaticPermission { Name = "book:read", Description = "Read book details", Category = "Books" },
            new StaticPermission { Name = "book:update", Description = "Update book", Category = "Books" },
            new StaticPermission { Name = "book:delete", Description = "Delete book", Category = "Books" },
            new StaticPermission { Name = "book:list", Description = "view book", Category = "Books" },

            new StaticPermission { Name = "librarymember:lookup", Description = "Lookup librarymember", Category = "LibraryMember" },
            new StaticPermission { Name = "librarymember:create", Description = "Create librarymember", Category = "LibraryMember" },
            new StaticPermission { Name = "librarymember:read", Description = "Read book librarymember", Category = "LibraryMember" },
            new StaticPermission { Name = "librarymember:update", Description = "Update librarymember", Category = "LibraryMember" },
            new StaticPermission { Name = "librarymember:delete", Description = "Delete librarymember", Category = "LibraryMember" },
            new StaticPermission { Name = "librarymember:list", Description = "view librarymember", Category = "LibraryMember" },
            new StaticPermission { Name = "librarymember:approve", Description = "Approved librarymember", Category = "LibraryMember" },
            new StaticPermission { Name = "librarymember:reject", Description = "rejected librarymember", Category = "LibraryMember" },
            new StaticPermission { Name = "librarymember:cancel", Description = "cancelled librarymember", Category = "LibraryMember" },
            new StaticPermission { Name = "librarymember:request", Description = "Request librarymember", Category = "LibraryMember" },
            new StaticPermission { Name = "librarymember:requestall", Description = "All Request librarymember", Category = "LibraryMember" },
            new StaticPermission { Name = "librarymember:updateanddeleteall", Description = "update and delete library member ", Category = "LibraryMember" },

            new StaticPermission { Name = "bookissue:lookup", Description = "Lookup bookissue", Category = "BookIssue" },
            new StaticPermission { Name = "bookissue:currentuser", Description = "List with current User", Category = "BookIssue" },
            new StaticPermission { Name = "bookissue:create", Description = "Create bookissue", Category = "BookIssue" },
            new StaticPermission { Name = "bookissue:read", Description = "Read book bookissue", Category = "BookIssue" },
            new StaticPermission { Name = "bookissue:update", Description = "Update bookissue", Category = "BookIssue" },
            new StaticPermission { Name = "bookissue:delete", Description = "Delete bookissue", Category = "BookIssue" },
            new StaticPermission { Name = "bookissue:list", Description = "view bookissue", Category = "BookIssue" },

            new StaticPermission { Name = "category:lookup", Description = "lookup category", Category = "Category" },
            new StaticPermission { Name = "category:create", Description = "Create category", Category = "Category" },
            new StaticPermission { Name = "category:read", Description = "Read book category", Category = "Category" },
            new StaticPermission { Name = "category:update", Description = "Update category", Category = "Category" },
            new StaticPermission { Name = "category:delete", Description = "Delete category", Category = "Category" },
            new StaticPermission { Name = "category:list", Description = "view category", Category = "Category" },
        };
    }
}