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
            // permission
            // new StaticPermission { Name = "permission:read", Description = "view or list permission", Category = "Permission" },

            // role
            new StaticPermission { Name = "role:list", Description = "list role", Category = "Roles" },
            new StaticPermission { Name = "role:create", Description = "Create role", Category = "Roles" },
            new StaticPermission { Name = "role:read", Description = "Read role details", Category = "Roles" },
            new StaticPermission { Name = "role:update", Description = "Update role", Category = "Roles" },
            new StaticPermission { Name = "role:delete", Description = "Delete role", Category = "Roles" },
            new StaticPermission { Name = "role:assign-permissions", Description = "Assign permissions to role", Category = "Roles" },

            // user 
            // new StaticPermission { Name = "user:lookup", Description = "lookup User", Category = "Users" },
            new StaticPermission { Name = "user:list", Description = "list User", Category = "Users" },
            new StaticPermission { Name = "user:create", Description = "Create User", Category = "Users" },
            new StaticPermission { Name = "user:read", Description = "Read User details", Category = "Users" },
            new StaticPermission { Name = "user:update", Description = "Update User", Category = "Users" },
            new StaticPermission { Name = "user:delete", Description = "Delete User", Category = "Users" },
            new StaticPermission { Name = "user:assign-roles", Description = "Assign roles to User", Category = "Users" },

            // category 
            // new StaticPermission { Name = "category:lookup", Description = "Lookup category", Category = "Category" },
            new StaticPermission { Name = "category:create", Description = "Create category", Category = "Category" },
            new StaticPermission { Name = "category:read", Description = "List category", Category = "Category" },
            new StaticPermission { Name = "category:update", Description = "Update category", Category = "Category" },
            new StaticPermission { Name = "category:delete", Description = "Delete category", Category = "Category" },
            new StaticPermission { Name = "category:view", Description = "View category detail", Category = "Category" },

            // product
            // new StaticPermission { Name = "product:sale_pos", Description = "Sale Product at POS", Category = "Products" },
            new StaticPermission { Name = "product:create", Description = "Create product", Category = "Products" },
            new StaticPermission { Name = "product:read", Description = "List product", Category = "Products" },
            new StaticPermission { Name = "product:update", Description = "Update product", Category = "Products" },
            new StaticPermission { Name = "product:delete", Description = "Delete product", Category = "Products" },
            new StaticPermission { Name = "product:view", Description = "View product detail", Category = "Products" },

            // order 
            new StaticPermission { Name = "order:read", Description = "View all order list", Category = "Order" },
            new StaticPermission { Name = "order:create", Description = "Order or sale product", Category = "Order" },
            new StaticPermission { Name = "order:refound", Description = "Refound Order", Category = "Order" },
            new StaticPermission { Name = "order:view", Description = "View order detail", Category = "Order" },

            // leave request 
            new StaticPermission { Name = "leave_request:create", Description = "Create My Leave Request", Category = "Leave_Request" },
            new StaticPermission { Name = "leave_request:read_my", Description = "List My Leave Request", Category = "Leave_Request" },
            new StaticPermission { Name = "leave_request:read_all", Description = "List All Leave Request", Category = "Leave_Request" },
            new StaticPermission { Name = "leave_request:view", Description = "View Detail Leave Request", Category = "Leave_Request" },
            new StaticPermission { Name = "leave_request:approve", Description = "Approve Leave Request", Category = "Leave_Request" },
            new StaticPermission { Name = "leave_request:reject", Description = "Reject Leave Request", Category = "Leave_Request" },
            new StaticPermission { Name = "leave_request:cancel", Description = "Cancel Leave Request", Category = "Leave_Request" },

            // leave type 
            // new StaticPermission { Name = "leave_type:lookup", Description = "Lookup Leave Type", Category = "Leave_Type" },
            new StaticPermission { Name = "leave_type:create", Description = "Create Leave Type", Category = "Leave_Type" },
            new StaticPermission { Name = "leave_type:read", Description = "List Leave Type", Category = "Leave_Type" },
            new StaticPermission { Name = "leave_type:update", Description = "Update Leave Type", Category = "Leave_Type" },
            new StaticPermission { Name = "leave_type:delete", Description = "Delete Leave Type", Category = "Leave_Type" },
            new StaticPermission { Name = "leave_type:view", Description = "View Leave Type Detail", Category = "Leave_Type" },

            // branch 
            // new StaticPermission { Name = "branch:lookup", Description = "Lookup Branch", Category = "Branch" },
            new StaticPermission { Name = "branch:create", Description = "Create Branch", Category = "Branch" },
            new StaticPermission { Name = "branch:read", Description = "List Branch", Category = "Branch" },
            new StaticPermission { Name = "branch:update", Description = "Update Branch", Category = "Branch" },
            new StaticPermission { Name = "branch:delete", Description = "Delete Branch", Category = "Branch" },
            new StaticPermission { Name = "branch:view", Description = "View Branch Detail", Category = "Branch" },

            // customer
            // new StaticPermission { Name = "customer:lookup", Description = "Lookup Customer", Category = "Customer" },
            new StaticPermission { Name = "customer:create", Description = "Create Customer", Category = "Customer" },
            new StaticPermission { Name = "customer:read", Description = "List Customer", Category = "Customer" },
            new StaticPermission { Name = "customer:update", Description = "Update Customer", Category = "Customer" },
            new StaticPermission { Name = "customer:delete", Description = "Delete Customer", Category = "Customer" },
            new StaticPermission { Name = "customer:view", Description = "View Customer Detail", Category = "Customer" },

            // staff
            // new StaticPermission { Name = "staff:lookup", Description = "Lookup Staff", Category = "Staff" },
            new StaticPermission { Name = "staff:create", Description = "Create Staff", Category = "Staff" },
            new StaticPermission { Name = "staff:read", Description = "List Staff", Category = "Staff" },
            new StaticPermission { Name = "staff:update", Description = "Update Staff", Category = "Staff" },
            new StaticPermission { Name = "staff:delete", Description = "Delete Staff", Category = "Staff" },
            new StaticPermission { Name = "staff:view", Description = "View Staff Detail", Category = "Staff" },

            // discount
            // new StaticPermission { Name = "discount:lookup", Description = "Lookup Discount", Category = "Discount" },
            new StaticPermission { Name = "discount:create", Description = "Create Discount", Category = "Discount" },
            new StaticPermission { Name = "discount:read", Description = "List Discount", Category = "Discount" },
            new StaticPermission { Name = "discount:update", Description = "Update Discount", Category = "Discount" },
            new StaticPermission { Name = "discount:delete", Description = "Delete Discount", Category = "Discount" },
            new StaticPermission { Name = "discount:view", Description = "View Discount Detail", Category = "Discount" },

            // manage stock
            new StaticPermission { Name = "manage_stock:all", Description = "Manage Stock", Category = "Manage_Stock" },

            // point setting
            new StaticPermission { Name = "point_setting:view", Description = "View Point Setting", Category = "Point_Setting" },
            new StaticPermission { Name = "point_setting:update", Description = "Update Point Setting", Category = "Point_Setting" },

        };
    }
}