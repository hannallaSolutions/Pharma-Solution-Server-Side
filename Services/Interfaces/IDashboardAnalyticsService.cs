using SearchTool_ServerSide.Dtos.DashboardDto;

namespace SearchTool_ServerSide.Services.Interfaces
{
    public interface IDashboardAnalyticsService
    {
        Task<List<ScriptAnalyticsDto>> GetScriptsAsync(
            DateTime? dateFrom,
            DateTime? dateTo,
            int? branchId,
            bool allBranches,
            CancellationToken ct);
    }
}
