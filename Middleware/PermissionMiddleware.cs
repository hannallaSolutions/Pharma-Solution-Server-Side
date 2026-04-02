using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SearchTool_ServerSide.Authentication;
using SearchTool_ServerSide.Data;
using SearchTool_ServerSide.Models;

namespace SearchTool_ServerSide.Middleware
{
    public class PermissionMiddleware
    {
        private readonly RequestDelegate _next;

        public PermissionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context,
            SearchToolDBContext db,
            UserAccessToken userAccessToken)
        {
            var endpoint = context.GetEndpoint();

            if (endpoint == null)
            {
                await _next(context);
                return;
            }

            if (endpoint.Metadata.GetMetadata<IAllowAnonymous>() != null)
            {
                await _next(context);
                return;
            }

            var hasAuthorize = endpoint.Metadata.GetMetadata<IAuthorizeData>() != null;
            if (!hasAuthorize)
            {
                await _next(context);
                return;
            }

            if (context.User?.Identity?.IsAuthenticated != true)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Unauthorized");
                return;
            }

            var token = userAccessToken.tokenData();
            if (token == null || string.IsNullOrWhiteSpace(token.UserRole))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Invalid token data.");
                return;
            }

            if (!Enum.TryParse<Role>(token.UserRole, true, out var parsedRole))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Invalid role.");
                return;
            }

            var requestPath = NormalizePath(context.Request.Path.Value ?? "");
            var requestMethod = (context.Request.Method ?? "").Trim().ToUpperInvariant();

            Console.WriteLine("=== Permission Middleware ===");
            Console.WriteLine("Path: " + requestPath);
            Console.WriteLine("Method: " + requestMethod);
            Console.WriteLine("Role: " + token.UserRole);

            var permissions = await db.Permissions
                .AsNoTracking()
                .ToListAsync();

            var permission = permissions.FirstOrDefault(p =>
                !string.IsNullOrWhiteSpace(p.Url) &&
                !string.IsNullOrWhiteSpace(p.HttpMethod) &&
                NormalizePath(p.Url) == requestPath &&
                p.HttpMethod.Trim().ToUpperInvariant() == requestMethod);

            if (permission == null)
            {
              

                 await _next(context);
    return;

            }

            var hasPermission = await db.RolePermissions
                .AsNoTracking()
                .AnyAsync(rp => rp.Role == parsedRole && rp.PermissionId == permission.Id);

            Console.WriteLine("Permission Found: " + permission.Name);
            Console.WriteLine("Has Permission: " + hasPermission);

            if (!hasPermission)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Forbidden");
                return;
            }

            await _next(context);
        }

        private static string NormalizePath(string path)
        {
            return path.Trim().TrimEnd('/').ToLowerInvariant();
        }
    }
}