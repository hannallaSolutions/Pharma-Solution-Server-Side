using Microsoft.EntityFrameworkCore;
using SearchTool_ServerSide.Data;
using SearchTool_ServerSide.Dtos.BranchIntelligenceDto;
using SearchTool_ServerSide.Models;

namespace SearchTool_ServerSide.Repository
{
    public class BranchIntelligenceRepository
    {
        private readonly SearchToolDBContext _context;

        public BranchIntelligenceRepository(SearchToolDBContext context)
        {
            _context = context;
        }

        public async Task<List<BranchLeaderboardRowDto>> GetBranchLeaderboardRawAsync(
            IReadOnlyCollection<int> accessibleBranchIds,
            DateTime? dateFrom,
            DateTime? dateTo,
            CancellationToken ct)
        {
            if (accessibleBranchIds == null || accessibleBranchIds.Count == 0)
            {
                return new List<BranchLeaderboardRowDto>();
            }

            var query = BaseQuery(dateFrom, dateTo)
                .Where(i => accessibleBranchIds.Contains(i.Script.BranchId));

            var rows = await query
                .GroupBy(i => new { i.Script.BranchId, i.Script.Branch.Code, i.Script.Branch.Name })
                .Select(g => new BranchLeaderboardRowDto
                {
                    BranchId = g.Key.BranchId,
                    BranchCode = g.Key.Code,
                    BranchName = g.Key.Name,
                    TotalScripts = g.Count(),
                    TotalNetProfit = g.Sum(x => (x.InsurancePayment + x.PatientPayment) - x.AcquisitionCost),
                    NegativeScriptCount = g.Count(x => (x.InsurancePayment + x.PatientPayment) - x.AcquisitionCost < 0)
                })
                .ToListAsync(ct);

            return rows;
        }

        public async Task<(string? Code, string? Name)?> GetBranchMetaAsync(int branchId, CancellationToken ct)
        {
            var meta = await _context.Branches
                .AsNoTracking()
                .Where(b => b.Id == branchId)
                .Select(b => new { b.Code, b.Name })
                .FirstOrDefaultAsync(ct);

            return meta == null ? null : (meta.Code, meta.Name);
        }

        public async Task<BranchOverviewAggregateDto> GetBranchOverviewAsync(
            int branchId,
            DateTime? dateFrom,
            DateTime? dateTo,
            CancellationToken ct)
        {
            var agg = await BranchQuery(branchId, dateFrom, dateTo)
                .GroupBy(i => 1)
                .Select(g => new BranchOverviewAggregateDto
                {
                    TotalScripts = g.Count(),
                    TotalNetProfit = g.Sum(x => (x.InsurancePayment + x.PatientPayment) - x.AcquisitionCost),
                    NegativeScriptCount = g.Count(x => (x.InsurancePayment + x.PatientPayment) - x.AcquisitionCost < 0)
                })
                .FirstOrDefaultAsync(ct);

            return agg ?? new BranchOverviewAggregateDto();
        }

        public async Task<List<BranchTopDrugDto>> GetTopDrugsAsync(
            int branchId,
            DateTime? dateFrom,
            DateTime? dateTo,
            int take,
            CancellationToken ct)
        {
            var rows = await BranchQuery(branchId, dateFrom, dateTo)
                .Where(i => i.Drug != null)
                .GroupBy(i => new { i.DrugId, i.Drug.Name })
                .Select(g => new BranchTopDrugDto
                {
                    DrugName = g.Key.Name,
                    TotalScripts = g.Count(),
                    TotalNetProfit = g.Sum(x => (x.InsurancePayment + x.PatientPayment) - x.AcquisitionCost)
                })
                .OrderByDescending(x => x.TotalNetProfit)
                .Take(take)
                .ToListAsync(ct);

            return rows;
        }

        public async Task<List<BranchTopTherapeuticClassDto>> GetTopTherapeuticClassesAsync(
            int branchId,
            DateTime? dateFrom,
            DateTime? dateTo,
            int take,
            CancellationToken ct)
        {
            // Mirrors the ClassV1 resolution used by DashboardAnalyticsRepository's
            // ScriptAnalyticsDto.DrugClass projection.
            var projected = BranchQuery(branchId, dateFrom, dateTo)
                .Select(i => new
                {
                    ClassName = i.Drug.DrugClasses
                        .Where(dc => EF.Functions.ILike(dc.ClassInfo.ClassType.Name, "ClassV1"))
                        .OrderBy(dc => dc.ClassInfo.Id)
                        .Select(dc => dc.ClassInfo.Name)
                        .FirstOrDefault(),
                    NetProfit = (i.InsurancePayment + i.PatientPayment) - i.AcquisitionCost
                });

            var rows = await projected
                .Where(x => x.ClassName != null)
                .GroupBy(x => x.ClassName)
                .Select(g => new BranchTopTherapeuticClassDto
                {
                    ClassName = g.Key!,
                    TotalScripts = g.Count(),
                    TotalNetProfit = g.Sum(x => x.NetProfit)
                })
                .OrderByDescending(x => x.TotalNetProfit)
                .Take(take)
                .ToListAsync(ct);

            return rows;
        }

        public async Task<List<BranchMonthlyProfitPointDto>> GetMonthlyNetProfitTrendAsync(
            int branchId,
            DateTime? dateFrom,
            DateTime? dateTo,
            CancellationToken ct)
        {
            var rows = await BranchQuery(branchId, dateFrom, dateTo)
                .GroupBy(i => new { i.Script.Date.Year, i.Script.Date.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    NetProfit = g.Sum(x => (x.InsurancePayment + x.PatientPayment) - x.AcquisitionCost),
                    TotalScripts = g.Count()
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync(ct);

            return rows
                .Select(x => new BranchMonthlyProfitPointDto
                {
                    Month = $"{x.Year:0000}-{x.Month:00}",
                    NetProfit = x.NetProfit,
                    TotalScripts = x.TotalScripts
                })
                .ToList();
        }

        private IQueryable<ScriptItem> BaseQuery(DateTime? dateFrom, DateTime? dateTo)
        {
            var query = _context.ScriptItems.AsNoTracking().AsQueryable();

            if (dateFrom.HasValue)
            {
                query = query.Where(i => i.Script.Date >= dateFrom.Value);
            }

            if (dateTo.HasValue)
            {
                query = query.Where(i => i.Script.Date <= dateTo.Value);
            }

            return query;
        }

        private IQueryable<ScriptItem> BranchQuery(int branchId, DateTime? dateFrom, DateTime? dateTo)
        {
            return BaseQuery(dateFrom, dateTo).Where(i => i.Script.BranchId == branchId);
        }
    }
}
