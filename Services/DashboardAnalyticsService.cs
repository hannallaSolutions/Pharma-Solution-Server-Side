using Microsoft.EntityFrameworkCore;
using SearchTool_ServerSide.Authentication;
using SearchTool_ServerSide.Data;
using SearchTool_ServerSide.Dtos.DashboardDto;
using SearchTool_ServerSide.Repository.Interfaces;
using SearchTool_ServerSide.Services.Interfaces;

namespace SearchTool_ServerSide.Services
{
    public class DashboardAnalyticsService : IDashboardAnalyticsService
    {
        private readonly IDashboardAnalyticsRepository _repository;
        private readonly UserAccessToken _userAccessToken;
        private readonly SearchToolDBContext _context;

        public DashboardAnalyticsService(
            IDashboardAnalyticsRepository repository,
            UserAccessToken userAccessToken,
            SearchToolDBContext context)
        {
            _repository = repository;
            _userAccessToken = userAccessToken;
            _context = context;
        }

        public async Task<List<ScriptAnalyticsDto>> GetScriptsAsync(
            DateTime? dateFrom,
            DateTime? dateTo,
            int? branchId,
            bool allBranches,
            CancellationToken ct)
        {
            var token = _userAccessToken.tokenData();
            if (token == null || !int.TryParse(token.UserId, out int userId))
            {
                return new List<ScriptAnalyticsDto>();
            }

            bool isSuperAdmin = string.Equals(token.UserRole, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
            int? tokenBranchId = int.TryParse(token.BranchId, out int parsedTokenBranchId) ? parsedTokenBranchId : (int?)null;

            if (allBranches)
            {
                // allBranches=true always wins over any submitted branchId, and always means
                // "every authorized branch under the caller's own Main Company" - for both
                // SuperAdmin and non-SuperAdmin - never system-wide.
                var authorizedBranchIds = await GetAllBranchesAuthorizedIdsAsync(userId, isSuperAdmin, tokenBranchId, ct);

                if (authorizedBranchIds.Count == 0)
                {
                    return new List<ScriptAnalyticsDto>();
                }

                return await _repository.GetScriptsAsync(authorizedBranchIds, dateFrom, dateTo, null, ct);
            }

            var accessibleBranchIds = await GetAccessibleBranchIdsAsync(userId, isSuperAdmin, tokenBranchId, ct);

            // Default to the user's current branch when no branchId query param was sent,
            // so two users scoped to different branches never see the same combined total.
            int? effectiveBranchId = branchId ?? tokenBranchId;

            // TEMP-LOG: diagnostics for the shared-branch-count investigation. Remove once verified in prod.
            Console.WriteLine(
                $"TEMP-LOG [DashboardAnalyticsService.GetScriptsAsync] userId={userId} role={token.UserRole} " +
                $"tokenBranchId={(tokenBranchId?.ToString() ?? "null")} requestedBranchId={(branchId?.ToString() ?? "null")} " +
                $"effectiveBranchId={(effectiveBranchId?.ToString() ?? "null")} " +
                $"accessibleBranchIds=[{string.Join(",", accessibleBranchIds)}]");

            if (effectiveBranchId.HasValue)
            {
                if (!isSuperAdmin && !accessibleBranchIds.Contains(effectiveBranchId.Value))
                {
                    return new List<ScriptAnalyticsDto>();
                }

                return await _repository.GetScriptsAsync(accessibleBranchIds, dateFrom, dateTo, effectiveBranchId, ct);
            }

            // No branchId param and no current branch on the token: only a true SuperAdmin
            // may fall back to every accessible branch; everyone else gets nothing rather
            // than an unscoped, cross-branch total.
            if (!isSuperAdmin)
            {
                return new List<ScriptAnalyticsDto>();
            }

            return await _repository.GetScriptsAsync(accessibleBranchIds, dateFrom, dateTo, null, ct);
        }

        // Mirrors CurrentBranchService.ResolveAsync(): SuperAdmin sees every branch,
        // everyone else is scoped to their active UserBranches assignments, falling
        // back to the single legacy User.BranchId for users not yet migrated.
        private async Task<List<int>> GetAccessibleBranchIdsAsync(int userId, bool isSuperAdmin, int? tokenBranchId, CancellationToken ct)
        {
            if (isSuperAdmin)
            {
                return await _context.Branches.Select(b => b.Id).ToListAsync(ct);
            }

            var branchIds = await _context.UserBranches
                .Where(ub => ub.UserId == userId && ub.IsActive)
                .Select(ub => ub.BranchId)
                .ToListAsync(ct);

            if (branchIds.Count == 0 && tokenBranchId.HasValue)
            {
                branchIds.Add(tokenBranchId.Value);
            }

            return branchIds;
        }

        // "All Branches" always means every authorized branch under the caller's own Main
        // Company (resolved from their current/token branch) - never system-wide, even for
        // SuperAdmin, and never broader than a non-SuperAdmin's actual UserBranches assignments.
        private async Task<List<int>> GetAllBranchesAuthorizedIdsAsync(int userId, bool isSuperAdmin, int? tokenBranchId, CancellationToken ct)
        {
            if (!tokenBranchId.HasValue)
            {
                return new List<int>();
            }

            int? mainCompanyId = await _context.Branches
                .Where(b => b.Id == tokenBranchId.Value)
                .Select(b => (int?)b.MainCompanyId)
                .FirstOrDefaultAsync(ct);

            if (!mainCompanyId.HasValue)
            {
                return new List<int>();
            }

            if (isSuperAdmin)
            {
                return await _context.Branches
                    .Where(b => b.MainCompanyId == mainCompanyId.Value)
                    .Select(b => b.Id)
                    .ToListAsync(ct);
            }

            // Force the assignment-based branch of GetAccessibleBranchIdsAsync (isSuperAdmin: false)
            // so we get the user's actual UserBranches assignments, with legacy User.BranchId
            // fallback - never the SuperAdmin system-wide bypass.
            var assignedBranchIds = await GetAccessibleBranchIdsAsync(userId, isSuperAdmin: false, tokenBranchId, ct);

            // Main Company is an intersecting safety boundary only - it can narrow
            // assignedBranchIds, never add a branch the user wasn't already assigned to.
            return await _context.Branches
                .Where(b => assignedBranchIds.Contains(b.Id) && b.MainCompanyId == mainCompanyId.Value)
                .Select(b => b.Id)
                .ToListAsync(ct);
        }
    }
}
