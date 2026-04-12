namespace POS.Application.Features.Customer
{
    public class CustomerInfo
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string ImageProfile { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public int TotalPoint { get; set; }
        public bool Status { get; set; }

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public DateTimeOffset? UpdatedDate { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTimeOffset? DeletedDate { get; set; }
        public string? DeletedBy { get; set; }
        public LinkedUserInfo? User {get; set;}
    }


    public class LinkedUserInfo
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}