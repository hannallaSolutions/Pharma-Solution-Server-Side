using SearchTool_ServerSide.Dtos.DashboardDto;

namespace SearchTool_ServerSide.Repository.Interfaces
{
    public interface IDashboardAnalyticsRepository
    {
        Task<List<ScriptAnalyticsDto>> GetScriptsAsync(
            IReadOnlyCollection<int> accessibleBranchIds,
            DateTime? dateFrom,
            DateTime? dateTo,
            int? branchId,
            CancellationToken ct);
    }
}
