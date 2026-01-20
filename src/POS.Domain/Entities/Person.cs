// POS.Domain/Entities/Person.cs
using POS.Domain.Common;
using System;
using System.Collections.Generic;

namespace POS.Domain.Entities
{
    public class Person : BaseAuditableEntity
    {
        public string ImageProfile { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public ICollection<PersonRole> PersonRoles { get; set; } = new List<PersonRole>();
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}
