using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;

namespace POS.API.Authorization
{
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            // Check if user is authenticated
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                System.Console.WriteLine($"❌ User is NOT authenticated");
                return Task.CompletedTask;
            }

            System.Console.WriteLine($"✅ User IS authenticated: {context.User.Identity.Name}");

            // Get ALL permission claims
            var permissionClaims = context.User.Claims
                .Where(c => c.Type == "permission")
                .ToList();

            System.Console.WriteLine($"📋 Found {permissionClaims.Count} permission claims");

            if (!permissionClaims.Any())
            {
                System.Console.WriteLine($"❌ No permission claims found");
                return Task.CompletedTask;
            }

            var permissions = new List<string>();

            foreach (var claim in permissionClaims)
            {
                System.Console.WriteLine($"🔍 Processing claim value: {claim.Value}");
                
                // Try to parse as JSON array (if it's stored as array)
                if (claim.Value.StartsWith("["))
                {
                    try
                    {
                        var arrayPermissions = JsonSerializer.Deserialize<List<string>>(claim.Value);
                        if (arrayPermissions != null)
                        {
                            permissions.AddRange(arrayPermissions);
                            System.Console.WriteLine($"✅ Parsed {arrayPermissions.Count} permissions from JSON array");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Console.WriteLine($"❌ Failed to parse JSON: {ex.Message}");
                        permissions.Add(claim.Value);
                    }
                }
                else
                {
                    // Single permission value
                    permissions.Add(claim.Value);
                    System.Console.WriteLine($"✅ Added single permission: {claim.Value}");
                }
            }

            System.Console.WriteLine($"📋 Total permissions: {string.Join(", ", permissions)}");
            System.Console.WriteLine($"🔐 Required permission: {requirement.Permission}");

            // Check if user has the required permission
            if (permissions.Contains(requirement.Permission))
            {
                System.Console.WriteLine($"✅ Permission GRANTED!");
                context.Succeed(requirement);
            }
            else
            {
                System.Console.WriteLine($"❌ Permission DENIED - user doesn't have '{requirement.Permission}'");
            }

            return Task.CompletedTask;
        }
    }
}