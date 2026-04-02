using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SearchTool_ServerSide.Data;
using SearchTool_ServerSide.Models;
using System.Security.Claims;

namespace SearchTool_ServerSide.Authorization
{
    public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly SearchToolDBContext _db;

        public PermissionHandler(SearchToolDBContext db)
        {
            _db = db;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            var roleClaim =
                context.User.FindFirst(ClaimTypes.Role)?.Value ??
                context.User.FindFirst("role")?.Value;

            if (string.IsNullOrWhiteSpace(roleClaim))
                return;

            if (!Enum.TryParse<Role>(roleClaim, true, out var parsedRole))
                return;

            var hasPermission = await _db.RolePermissions
                .Include(rp => rp.Permission)
                .AnyAsync(rp =>
                    rp.Role == parsedRole &&
                    rp.Permission.Name == requirement.PermissionName);

            if (hasPermission)
            {
                context.Succeed(requirement);
            }
        }
    }
}