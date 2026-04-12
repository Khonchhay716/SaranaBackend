// namespace POS.Application.Features.Staff
// {
//     public class StaffInfo
//     {
//         public int Id { get; set; }
//         public string FirstName { get; set; } = string.Empty;
//         public string LastName { get; set; } = string.Empty;
//         public string ImageProfile { get; set; } = string.Empty;
//         public string PhoneNumber { get; set; } = string.Empty;
//         public string Position { get; set; } = string.Empty;
//         public decimal Salary { get; set; }
//         public bool Status { get; set; } = true;
//         public bool IsDeleted { get; set; }

//         public DateTimeOffset CreatedDate { get; set; }
//         public string? CreatedBy { get; set; }
//         public DateTimeOffset? UpdatedDate { get; set; }
//         public string? UpdatedBy { get; set; }
//         public DateTimeOffset? DeletedDate { get; set; }
//         public string? DeletedBy { get; set; }
//         public LinkedUserInfo? User { get; set; }
//     }

//     public class LinkedUserInfo
//     {
//         public int Id { get; set; }
//         public string Username { get; set; } = string.Empty;
//         public string Email { get; set; } = string.Empty;
//         public bool IsActive { get; set; }
//     }
// }



namespace POS.Application.Features.Staff
{
    public class StaffInfo
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string ImageProfile { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public decimal Salary { get; set; }
        public bool Status { get; set; } = true;
        public bool IsDeleted { get; set; }
        public int? SupervisorId { get; set; }
        public SupervisorInfo? Supervisor { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public DateTimeOffset? UpdatedDate { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTimeOffset? DeletedDate { get; set; }
        public string? DeletedBy { get; set; }
        public LinkedUserInfo? User { get; set; }
    }

    public class SupervisorInfo
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string ImageProfile { get; set; } = string.Empty;
    }

    public class LinkedUserInfo
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}