using Microsoft.EntityFrameworkCore;
using SearchTool_ServerSide.Authentication;
using SearchTool_ServerSide.Data;

namespace SearchTool_ServerSide.Services
{
    public class CurrentBranchResult
    {
        public bool Success { get; set; }
        public int BranchId { get; set; }
        public string Source { get; set; } = "";
        public string? Error { get; set; }
        public int StatusCode { get; set; }
    }

    public class CurrentBranchService(
        IHttpContextAccessor httpContextAccessor,
        UserAccessToken userAccessToken,
        SearchToolDBContext db)
    {
        public async Task<CurrentBranchResult> ResolveAsync()
        {
            var token = userAccessToken.tokenData();
            if (token == null)
                return Fail("Unauthorized", 401);

            if (!int.TryParse(token.UserId, out int userId))
                return Fail("Invalid token", 401);

            var header = httpContextAccessor.HttpContext?
                .Request.Headers["X-Branch-Id"].FirstOrDefault();

            if (string.IsNullOrEmpty(header))
            {
                if (!int.TryParse(token.BranchId, out int jwtBranchId))
                    return Fail("Invalid branch in token", 400);
                return Ok(jwtBranchId, "JwtFallback");
            }

            if (!int.TryParse(header, out int requestedBranchId))
                return Fail("Invalid X-Branch-Id header value", 400);

            if (token.UserRole?.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) == true)
                return Ok(requestedBranchId, "Header");

            var isAssigned = await db.UserBranches.AnyAsync(ub =>
                ub.UserId == userId &&
                ub.BranchId == requestedBranchId &&
                ub.IsActive);

            if (!isAssigned)
                return Fail("User does not have access to the requested branch", 403);

            return Ok(requestedBranchId, "Header");
        }

        private static CurrentBranchResult Ok(int branchId, string source) =>
            new() { Success = true, BranchId = branchId, Source = source, StatusCode = 200 };

        private static CurrentBranchResult Fail(string error, int statusCode) =>
            new() { Success = false, Error = error, StatusCode = statusCode };
    }
}
