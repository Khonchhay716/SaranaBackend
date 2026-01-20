// POS.API/Authorization/PermissionRequirement.cs
using Microsoft.AspNetCore.Authorization;

namespace POS.API.Authorization
{
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string Permission { get; }

        public PermissionRequirement(string permission)
        {
            Permission = permission;
        }
    }
}