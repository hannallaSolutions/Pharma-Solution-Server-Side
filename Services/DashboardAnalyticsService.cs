using Microsoft.EntityFrameworkCore;
using SearchTool_ServerSide.Authentication;
using SearchTool_ServerSide.Data;
using SearchTool_ServerSide.Dtos.DashboardDto;
using SearchTool_ServerSide.Repository;

namespace SearchTool_ServerSide.Services
{
    public class DashboardAnalyticsService
    {
        private readonly DashboardAnalyticsRepository _repository;
        private readonly UserAccessToken _userAccessToken;
        private readonly SearchToolDBContext _context;

        public DashboardAnalyticsService(
            DashboardAnalyticsRepository repository,
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

            bool isSuperAdmin = string.Equals(
                token.UserRole,
                "SuperAdmin",
                StringComparison.OrdinalIgnoreCase);

            bool isDemo = string.Equals(
                token.UserRole,
                "Demo",
                StringComparison.OrdinalIgnoreCase);

            int? tokenBranchId = int.TryParse(
                token.BranchId,
                out int parsedTokenBranchId)
                    ? parsedTokenBranchId
                    : null;

            if (allBranches)
            {
                // allBranches=true overrides any submitted branchId.
                //
                // SuperAdmin and Demo can retrieve all branches belonging to the
                // Main Company resolved from their current token branch.
                //
                // Other roles remain limited to their active UserBranches
                // assignments within that Main Company.
                var authorizedBranchIds =
                    await GetAllBranchesAuthorizedIdsAsync(
                        userId,
                        isSuperAdmin,
                        isDemo,
                        tokenBranchId,
                        ct);

                if (authorizedBranchIds.Count == 0)
                {
                    return new List<ScriptAnalyticsDto>();
                }

                return await _repository.GetScriptsAsync(
                    authorizedBranchIds,
                    dateFrom,
                    dateTo,
                    null,
                    ct);
            }

            // Normal branch-scoped behavior remains unchanged.
            // Demo does not receive special access here.
            var accessibleBranchIds =
                await GetAccessibleBranchIdsAsync(
                    userId,
                    isSuperAdmin,
                    tokenBranchId,
                    ct);

            // When no branchId is submitted, use the user's current/token branch.
            int? effectiveBranchId = branchId ?? tokenBranchId;

            if (effectiveBranchId.HasValue)
            {
                if (!isSuperAdmin &&
                    !accessibleBranchIds.Contains(effectiveBranchId.Value))
                {
                    return new List<ScriptAnalyticsDto>();
                }

                return await _repository.GetScriptsAsync(
                    accessibleBranchIds,
                    dateFrom,
                    dateTo,
                    effectiveBranchId,
                    ct);
            }

            // Without allBranches=true, a user with no branch in the token
            // must not receive an unscoped cross-branch dataset.
            if (!isSuperAdmin)
            {
                return new List<ScriptAnalyticsDto>();
            }

            return await _repository.GetScriptsAsync(
                accessibleBranchIds,
                dateFrom,
                dateTo,
                null,
                ct);
        }

        // Normal branch authorization:
        //
        // SuperAdmin can access every branch.
        // All other roles, including Demo, are limited to their active
        // UserBranches assignments.
        //
        // If the user has not yet been migrated to UserBranches, the method
        // falls back to the legacy branch stored in the access token.
        private async Task<List<int>> GetAccessibleBranchIdsAsync(
            int userId,
            bool isSuperAdmin,
            int? tokenBranchId,
            CancellationToken ct)
        {
            if (isSuperAdmin)
            {
                return await _context.Branches
                    .Select(b => b.Id)
                    .ToListAsync(ct);
            }

            var branchIds = await _context.UserBranches
                .Where(ub =>
                    ub.UserId == userId &&
                    ub.IsActive)
                .Select(ub => ub.BranchId)
                .Distinct()
                .ToListAsync(ct);

            if (branchIds.Count == 0 && tokenBranchId.HasValue)
            {
                branchIds.Add(tokenBranchId.Value);
            }

            return branchIds;
        }

        // Analytics-only "All Branches" authorization:
        //
        // The Main Company is resolved from the user's current/token branch.
        //
        // - SuperAdmin: all branches under that Main Company.
        // - Demo: all branches under that Main Company, temporarily for
        //   company-wide analytics access.
        // - Other roles: only their assigned UserBranches that also belong
        //   to that Main Company.
        //
        // This does not modify UserBranches and does not make Demo a real
        // multi-branch user outside this analytics flow.
        private async Task<List<int>> GetAllBranchesAuthorizedIdsAsync(
            int userId,
            bool isSuperAdmin,
            bool isDemo,
            int? tokenBranchId,
            CancellationToken ct)
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

            var companyBranchIds = await _context.Branches
                .Where(b => b.MainCompanyId == mainCompanyId.Value)
                .Select(b => b.Id)
                .Distinct()
                .ToListAsync(ct);

            if (isSuperAdmin || isDemo)
            {
                return companyBranchIds;
            }

            var assignedBranchIds = await _context.UserBranches
                .Where(ub =>
                    ub.UserId == userId &&
                    ub.IsActive)
                .Select(ub => ub.BranchId)
                .Distinct()
                .ToListAsync(ct);

            if (assignedBranchIds.Count == 0)
            {
                assignedBranchIds.Add(tokenBranchId.Value);
            }

            return companyBranchIds
                .Intersect(assignedBranchIds)
                .ToList();
        }
    }
}