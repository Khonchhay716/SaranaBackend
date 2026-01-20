using POS.Domain.Common;
using System.Collections.Generic;

namespace POS.Domain.Entities
{
    public class Role : BaseAuditableEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
        public ICollection<PersonRole> PersonRoles { get; set; } = new List<PersonRole>();
    }
}
