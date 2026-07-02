using System;

namespace POS.Domain.Common
{
    public abstract class BaseAuditableEntity
    {
        public int Id { get; set; }

        public DateTimeOffset CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public bool IsDeleted { get; set; }

        public DateTimeOffset? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }

        public DateTimeOffset? DeletedDate { get; set; }
        public int? DeletedBy { get; set; }
    }

    public abstract class BaseEntity
    {
        public int Id { get; set; }

        public DateTimeOffset CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public bool IsDeleted { get; set; }

        public DateTimeOffset? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }

        public DateTimeOffset? DeletedDate { get; set; }
        public int? DeletedBy { get; set; }
    }
}
