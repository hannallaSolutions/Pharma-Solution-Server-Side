using Microsoft.EntityFrameworkCore;
using SearchTool_ServerSide.Authentication;
using SearchTool_ServerSide.Data;
using SearchTool_ServerSide.Dtos.BranchIntelligenceDto;
using SearchTool_ServerSide.Repository;

namespace SearchTool_ServerSide.Services
{
    // Distinguishes "branch doesn't exist" (404) from "branch exists but is
    // outside this user's authorized Branch Intelligence scope" (403) — the
    // controller maps each value to the matching HTTP status.
    public enum BranchDetailAccessStatus
    {
        Ok,
        NotFound,
        Forbidden,
    }

    public class BranchIntelligenceService
    {
        private const int TopDrugsTake = 5;
        private const int TopClassesTake = 5;

        private readonly BranchIntelligenceRepository _repository;
        private readonly UserAccessToken _userAccessToken;
        private readonly SearchToolDBContext _context;

        public BranchIntelligenceService(
            BranchIntelligenceRepository repository,
            UserAccessToken userAccessToken,
            SearchToolDBContext context)
        {
            _repository = repository;
            _userAccessToken = userAccessToken;
            _context = context;
        }

        public async Task<BranchIntelligenceOverviewDto> GetOverviewAsync(
            DateTime? dateFrom,
            DateTime? dateTo,
            bool allBranches,
            CancellationToken ct)
        {
            var token = _userAccessToken.tokenData();

            if (token == null || !int.TryParse(token.UserId, out int userId))
            {
                return new BranchIntelligenceOverviewDto();
            }

            bool isSuperAdmin = string.Equals(token.UserRole, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
            bool isDemo = string.Equals(token.UserRole, "Demo", StringComparison.OrdinalIgnoreCase);
            int? tokenBranchId = int.TryParse(token.BranchId, out int parsedTokenBranchId) ? parsedTokenBranchId : null;

            var accessibleBranchIds = allBranches
                ? await ResolveAuthorizedBranchIdsAsync(userId, isSuperAdmin, isDemo, tokenBranchId, ct)
                : await GetAccessibleBranchIdsAsync(userId, isSuperAdmin, tokenBranchId, ct);

            if (accessibleBranchIds.Count == 0)
            {
                return new BranchIntelligenceOverviewDto();
            }

            var rows = await _repository.GetBranchLeaderboardRawAsync(accessibleBranchIds, dateFrom, dateTo, ct);

            if (rows.Count == 0)
            {
                return new BranchIntelligenceOverviewDto();
            }

            decimal companyTotalScripts = rows.Sum(r => r.TotalScripts);
            decimal companyTotalProfit = rows.Sum(r => r.TotalNetProfit);

            foreach (var row in rows)
            {
                row.ProfitPerScript = row.TotalScripts > 0
                    ? row.TotalNetProfit / row.TotalScripts
                    : (decimal?)null;

                row.NegativeScriptPercent = row.TotalScripts > 0
                    ? Math.Round((decimal)row.NegativeScriptCount / row.TotalScripts * 100m, 2)
                    : 0m;

                row.ShareOfCompanyScripts = companyTotalScripts > 0
                    ? Math.Round(row.TotalScripts / companyTotalScripts * 100m, 2)
                    : 0m;

                row.ShareOfCompanyProfit = companyTotalProfit != 0
                    ? Math.Round(row.TotalNetProfit / companyTotalProfit * 100m, 2)
                    : 0m;
            }

            var leaderboard = rows.OrderByDescending(r => r.TotalNetProfit).ToList();

            return new BranchIntelligenceOverviewDto
            {
                Leaderboard = leaderboard,
                ExecutiveCards = BuildExecutiveCards(leaderboard, companyTotalScripts, companyTotalProfit),
                Insights = BuildInsights(leaderboard)
            };
        }

        public async Task<(BranchDetailAccessStatus Status, BranchDetailDto? Detail)> GetBranchDetailAsync(
            long branchId,
            DateTime? dateFrom,
            DateTime? dateTo,
            CancellationToken ct)
        {
            var token = _userAccessToken.tokenData();

            if (token == null || !int.TryParse(token.UserId, out int userId))
            {
                return (BranchDetailAccessStatus.Forbidden, null);
            }

            bool isSuperAdmin = string.Equals(token.UserRole, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
            bool isDemo = string.Equals(token.UserRole, "Demo", StringComparison.OrdinalIgnoreCase);
            int? tokenBranchId = int.TryParse(token.BranchId, out int parsedTokenBranchId) ? parsedTokenBranchId : null;

            int branchIdInt = (int)branchId;

            // Existence is checked independently of authorization so a branch
            // that genuinely doesn't exist always reports 404, even for a user
            // who wouldn't be authorized to see it anyway.
            var meta = await _repository.GetBranchMetaAsync(branchIdInt, ct);

            if (meta == null)
            {
                return (BranchDetailAccessStatus.NotFound, null);
            }

            // Same shared scope resolver the overview uses for allBranches=true
            // (GetOverviewAsync above) — this is what the requirement means by
            // "the same authorized branch scope" for both endpoints: the
            // overview always requests the full authorized scope, so detail
            // must authorize against that same set, not the narrower
            // token/current-branch-only GetAccessibleBranchIdsAsync scope.
            if (!isSuperAdmin)
            {
                var authorizedBranchIds = await ResolveAuthorizedBranchIdsAsync(userId, isSuperAdmin, isDemo, tokenBranchId, ct);
                if (!authorizedBranchIds.Contains(branchIdInt))
                {
                    return (BranchDetailAccessStatus.Forbidden, null);
                }
            }

            var overview = await _repository.GetBranchOverviewAsync(branchIdInt, dateFrom, dateTo, ct);
            var topDrugs = await _repository.GetTopDrugsAsync(branchIdInt, dateFrom, dateTo, TopDrugsTake, ct);
            var topClasses = await _repository.GetTopTherapeuticClassesAsync(branchIdInt, dateFrom, dateTo, TopClassesTake, ct);
            var monthlyTrend = await _repository.GetMonthlyNetProfitTrendAsync(branchIdInt, dateFrom, dateTo, ct);

            var detail = new BranchDetailDto
            {
                BranchId = branchId,
                BranchCode = meta.Value.Code,
                BranchName = meta.Value.Name,
                TotalScripts = overview.TotalScripts,
                TotalNetProfit = overview.TotalNetProfit,
                ProfitPerScript = overview.TotalScripts > 0
                    ? overview.TotalNetProfit / overview.TotalScripts
                    : (decimal?)null,
                NegativeScriptCount = overview.NegativeScriptCount,
                NegativeScriptPercent = overview.TotalScripts > 0
                    ? Math.Round((decimal)overview.NegativeScriptCount / overview.TotalScripts * 100m, 2)
                    : 0m,
                TopDrugs = topDrugs,
                TopTherapeuticClasses = topClasses,
                MonthlyNetProfitTrend = monthlyTrend
            };

            return (BranchDetailAccessStatus.Ok, detail);
        }

        // Deterministic selection rules — one branch per card. With a single
        // branch in scope, all four cards resolve to that same branch.
        private static List<BranchExecutiveCardDto> BuildExecutiveCards(
            List<BranchLeaderboardRowDto> rows,
            decimal companyTotalScripts,
            decimal companyTotalProfit)
        {
            var cards = new List<BranchExecutiveCardDto>();

            if (rows.Count == 0)
            {
                return cards;
            }

            decimal avgProfitPerScript = companyTotalScripts > 0
                ? companyTotalProfit / companyTotalScripts
                : 0m;

            // Best Performing Branch — highest total net profit.
            var best = rows.OrderByDescending(r => r.TotalNetProfit).First();
            string comparisonWord = (best.ProfitPerScript ?? 0m) >= avgProfitPerScript ? "above-average" : "below-average";
            cards.Add(new BranchExecutiveCardDto
            {
                CardType = "BestPerforming",
                BranchId = best.BranchId,
                BranchName = best.BranchName,
                PrimaryMetricLabel = "Total Net Profit",
                PrimaryMetricValue = best.TotalNetProfit,
                SecondaryMetricLabel = "Profit / Script",
                SecondaryMetricValue = best.ProfitPerScript,
                Explanation = $"{best.BranchName} generated the highest overall net profit (${best.TotalNetProfit:N0}) while maintaining {comparisonWord} profitability per prescription (${(best.ProfitPerScript ?? 0m):N2} vs a company average of ${avgProfitPerScript:N2})."
            });

            // Needs Attention — highest negative-profit script rate.
            var withScripts = rows.Where(r => r.TotalScripts > 0).ToList();
            if (withScripts.Count > 0)
            {
                var attention = withScripts.OrderByDescending(r => r.NegativeScriptPercent).First();
                cards.Add(new BranchExecutiveCardDto
                {
                    CardType = "NeedsAttention",
                    BranchId = attention.BranchId,
                    BranchName = attention.BranchName,
                    PrimaryMetricLabel = "Negative Script %",
                    PrimaryMetricValue = attention.NegativeScriptPercent,
                    SecondaryMetricLabel = "Total Scripts",
                    SecondaryMetricValue = attention.TotalScripts,
                    Explanation = $"{attention.BranchName} has the highest rate of negative-profit prescriptions at {attention.NegativeScriptPercent:N1}% ({attention.NegativeScriptCount} of {attention.TotalScripts} scripts), making it the branch most in need of operational review."
                });
            }

            // Highest Efficiency — highest profit per script.
            var eligibleForEfficiency = withScripts.Where(r => r.ProfitPerScript.HasValue).ToList();
            if (eligibleForEfficiency.Count > 0)
            {
                var efficient = eligibleForEfficiency.OrderByDescending(r => r.ProfitPerScript!.Value).First();
                decimal diffPct = avgProfitPerScript != 0
                    ? ((efficient.ProfitPerScript!.Value - avgProfitPerScript) / Math.Abs(avgProfitPerScript)) * 100m
                    : 0m;
                string diffText = diffPct >= 0 ? $"{diffPct:N0}% above" : $"{Math.Abs(diffPct):N0}% below";
                cards.Add(new BranchExecutiveCardDto
                {
                    CardType = "HighestEfficiency",
                    BranchId = efficient.BranchId,
                    BranchName = efficient.BranchName,
                    PrimaryMetricLabel = "Profit / Script",
                    PrimaryMetricValue = efficient.ProfitPerScript,
                    SecondaryMetricLabel = "Total Scripts",
                    SecondaryMetricValue = efficient.TotalScripts,
                    Explanation = $"{efficient.BranchName} achieves the highest profit per prescription (${efficient.ProfitPerScript:N2}), {diffText} the company average despite dispensing {efficient.TotalScripts:N0} scripts."
                });
            }

            // Highest Workload — highest total scripts.
            var workload = rows.OrderByDescending(r => r.TotalScripts).First();
            cards.Add(new BranchExecutiveCardDto
            {
                CardType = "HighestWorkload",
                BranchId = workload.BranchId,
                BranchName = workload.BranchName,
                PrimaryMetricLabel = "Total Scripts",
                PrimaryMetricValue = workload.TotalScripts,
                SecondaryMetricLabel = "Share of Company Scripts",
                SecondaryMetricValue = workload.ShareOfCompanyScripts,
                Explanation = $"{workload.BranchName} processes the highest prescription volume in the network ({workload.TotalScripts:N0} scripts, {workload.ShareOfCompanyScripts:N1}% of company total)."
            });

            return cards;
        }

        // Deterministic, rule-based observations. Every rule requires the
        // underlying comparison to be mathematically meaningful for the
        // branches present — with one branch (or one branch carrying all
        // scripts), cross-branch rules do not fire rather than fabricate a
        // trend that doesn't exist.
        private static List<string> BuildInsights(List<BranchLeaderboardRowDto> rows)
        {
            var insights = new List<string>();

            if (rows.Count == 0)
            {
                return insights;
            }

            var withScripts = rows.Where(r => r.TotalScripts > 0).ToList();

            // Volume vs. profit-share imbalance.
            var mostImbalanced = withScripts
                .OrderByDescending(r => r.ShareOfCompanyScripts - r.ShareOfCompanyProfit)
                .FirstOrDefault();
            if (mostImbalanced != null && (mostImbalanced.ShareOfCompanyScripts - mostImbalanced.ShareOfCompanyProfit) >= 10m)
            {
                insights.Add($"{mostImbalanced.BranchName} processes {mostImbalanced.ShareOfCompanyScripts:N0}% of company prescriptions while contributing only {mostImbalanced.ShareOfCompanyProfit:N0}% of total profit.");
            }

            if (withScripts.Count >= 2)
            {
                // Negative-profit-script outlier.
                var worstNegative = withScripts.OrderByDescending(r => r.NegativeScriptPercent).First();
                decimal avgNegativePercent = withScripts.Average(r => r.NegativeScriptPercent);
                if (worstNegative.NegativeScriptPercent > 0 && worstNegative.NegativeScriptPercent > avgNegativePercent)
                {
                    insights.Add($"{worstNegative.BranchName} has the highest percentage of negative-profit prescriptions across the network at {worstNegative.NegativeScriptPercent:N1}%.");
                }

                var eligibleForEfficiency = withScripts.Where(r => r.ProfitPerScript.HasValue).ToList();
                if (eligibleForEfficiency.Count >= 2)
                {
                    // Highest efficiency despite lower-than-average volume.
                    decimal avgScripts = withScripts.Average(r => (decimal)r.TotalScripts);
                    var mostEfficient = eligibleForEfficiency.OrderByDescending(r => r.ProfitPerScript!.Value).First();
                    if (mostEfficient.TotalScripts < avgScripts)
                    {
                        insights.Add($"{mostEfficient.BranchName} produces the highest average profit per prescription (${mostEfficient.ProfitPerScript:N2}) despite lower dispensing volume ({mostEfficient.TotalScripts:N0} scripts).");
                    }

                    // Efficiency spread across the network.
                    var maxEff = eligibleForEfficiency.OrderByDescending(r => r.ProfitPerScript!.Value).First();
                    var minEff = eligibleForEfficiency.OrderBy(r => r.ProfitPerScript!.Value).First();
                    if (maxEff.BranchId != minEff.BranchId)
                    {
                        decimal gap = (maxEff.ProfitPerScript ?? 0m) - (minEff.ProfitPerScript ?? 0m);
                        if (gap > 0)
                        {
                            insights.Add($"Profit per script ranges from ${minEff.ProfitPerScript:N2} at {minEff.BranchName} to ${maxEff.ProfitPerScript:N2} at {maxEff.BranchName}, a ${gap:N2} gap in per-prescription profitability across the network.");
                        }
                    }
                }

                // Profit concentration.
                var topProfit = rows.OrderByDescending(r => r.TotalNetProfit).First();
                if (topProfit.ShareOfCompanyProfit > 50m)
                {
                    insights.Add($"{topProfit.BranchName} alone contributes {topProfit.ShareOfCompanyProfit:N0}% of total company net profit across {rows.Count} branches.");
                }
            }
            else if (withScripts.Count == 1)
            {
                // Single-branch fallback — descriptive only, no cross-branch
                // comparison is implied since there is nothing to compare to.
                var only = withScripts[0];
                if (only.NegativeScriptPercent > 0)
                {
                    insights.Add($"{only.BranchName} has a negative-profit prescription rate of {only.NegativeScriptPercent:N1}% across {only.TotalScripts:N0} scripts.");
                }
                if (only.ProfitPerScript.HasValue)
                {
                    insights.Add($"{only.BranchName} averages ${only.ProfitPerScript:N2} in net profit per prescription across {only.TotalScripts:N0} scripts.");
                }
            }

            return insights.Take(6).ToList();
        }

        // Copied from DashboardAnalyticsService: no shared/reusable branch-access
        // helper exists in this codebase yet, so branch-authorization logic is
        // duplicated per analytics service to match the established pattern.
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
                .Where(ub => ub.UserId == userId && ub.IsActive)
                .Select(ub => ub.BranchId)
                .Distinct()
                .ToListAsync(ct);

            if (branchIds.Count == 0 && tokenBranchId.HasValue)
            {
                branchIds.Add(tokenBranchId.Value);
            }

            return branchIds;
        }

        // Shared Branch Intelligence scope resolver — the single source of
        // truth for "which branches can this user see on this dashboard".
        // Used by both GetOverviewAsync (allBranches=true) and
        // GetBranchDetailAsync so a branch present in the overview response
        // is always authorized for its own detail request.
        private async Task<List<int>> ResolveAuthorizedBranchIdsAsync(
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
                .Where(ub => ub.UserId == userId && ub.IsActive)
                .Select(ub => ub.BranchId)
                .Distinct()
                .ToListAsync(ct);

            if (assignedBranchIds.Count == 0)
            {
                assignedBranchIds.Add(tokenBranchId.Value);
            }

            return companyBranchIds.Intersect(assignedBranchIds).ToList();
        }
    }
}
