// POS.Domain/Entities/RefreshToken.cs
using System;

namespace POS.Domain.Entities
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public string Token { get; set; } = string.Empty;
        public int PersonId { get; set; }
        public Person Person { get; set; } = default!;
        public DateTime ExpiryDate { get; set; }
        public bool IsRevoked { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}