using System;

namespace POS.Domain.Common
{
    public abstract class BaseAuditableEntity
    {
        public int Id { get; set; }

        public DateTimeOffset CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public bool IsDeleted { get; set; }

        public DateTimeOffset? UpdatedDate { get; set; }
        public string? UpdatedBy { get; set; }

        public DateTimeOffset? DeletedDate { get; set; }
        public string? DeletedBy { get; set; }
    }
}
