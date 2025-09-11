namespace POS.Application.Features.User
{
    public class UserInfo
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
        public DateTimeOffset CreateDate { get; set; } = DateTime.UtcNow;
        public DateTimeOffset Updatedate { get; set; }
        public DateTimeOffset DeletedDate { get; set; }
    }
}