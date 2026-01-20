// POS.API/Attributes/RequirePermissionAttribute.cs
using Microsoft.AspNetCore.Authorization;

namespace POS.API.Attributes
{
    public class RequirePermissionAttribute : AuthorizeAttribute
    {
        public RequirePermissionAttribute(string permission) : base($"Permission:{permission}")
        {
        }
    }
}