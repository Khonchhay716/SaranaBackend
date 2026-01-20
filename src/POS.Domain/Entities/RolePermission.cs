namespace POS.Domain.Entities
{
    // Junction table for Role-Permission relationship
    public class RolePermission
    {
        public int RoleId { get; set; }
        public Role Role { get; set; } = null!;
        public string PermissionName { get; set; } = string.Empty;
    }

}