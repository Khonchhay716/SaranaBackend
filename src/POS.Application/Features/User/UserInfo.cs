// POS.Application/Features/User/UserInfo.cs
using System;
using System.Collections.Generic;

namespace POS.Application.Features.User
{
    public class UserInfo
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string ImageProfile { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; }
        public DateTimeOffset? CreatedDate { get; set; }
        public List<RoleBasicInfo> Roles { get; set; } = new();
    }

    public class UserDetailInfo : UserInfo
    {
        public List<string> Permissions { get; set; } = new();
    }

    public class RoleBasicInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}