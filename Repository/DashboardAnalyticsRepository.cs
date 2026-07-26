using Microsoft.EntityFrameworkCore;
using SearchTool_ServerSide.Data;
using SearchTool_ServerSide.Dtos.DashboardDto;
using SearchTool_ServerSide.Repository.Interfaces;

namespace SearchTool_ServerSide.Repository
{
    public class DashboardAnalyticsRepository : IDashboardAnalyticsRepository
    {
        private readonly SearchToolDBContext _context;

        public DashboardAnalyticsRepository(SearchToolDBContext context)
        {
            _context = context;
        }

        public async Task<List<ScriptAnalyticsDto>> GetScriptsAsync(
            IReadOnlyCollection<int> accessibleBranchIds,
            DateTime? dateFrom,
            DateTime? dateTo,
            int? branchId,
            CancellationToken ct)
        {
            if (accessibleBranchIds == null || accessibleBranchIds.Count == 0)
            {
                return new List<ScriptAnalyticsDto>();
            }

            var query = _context.ScriptItems
                .AsNoTracking()
                .Where(i => accessibleBranchIds.Contains(i.Script.BranchId));

            if (dateFrom.HasValue)
            {
                query = query.Where(i => i.Script.Date >= dateFrom.Value);
            }

            if (dateTo.HasValue)
            {
                query = query.Where(i => i.Script.Date <= dateTo.Value);
            }

            if (branchId.HasValue)
            {
                query = query.Where(i => i.Script.BranchId == branchId.Value);
            }

            var result = await query
                .Select(i => new ScriptAnalyticsDto
                {
                    // Identity
                    ScriptId = i.ScriptId,
                    ScriptCode = i.Script.ScriptCode,
                    RxNumber = i.RxNumber,
                    Date = i.Script.Date,

                    // Drug
                    DrugId = i.DrugId,
                    DrugName = i.Drug != null ? i.Drug.Name : null,
                    NdcCode = i.NDCCode,
                    DrugClass = i.Drug != null
                        ? i.Drug.DrugClasses
                            .Where(dc => EF.Functions.ILike(dc.ClassInfo.ClassType.Name, "ClassV1"))
                            .OrderBy(dc => dc.ClassInfo.Id) // stable ordering — mirrors GetDrugClassesByPCNPaginated
                            .Select(dc => dc.ClassInfo.Name)
                            .FirstOrDefault()
                        : null,

                    // Insurance (ScriptItem.Insurance is an InsuranceRx; the real Insurance
                    // record is reached via InsuranceRx -> InsurancePCN -> Insurance)
                    InsuranceId = i.InsuranceId,
                    InsuranceName = i.Insurance != null && i.Insurance.InsurancePCN != null && i.Insurance.InsurancePCN.Insurance != null
                        ? i.Insurance.InsurancePCN.Insurance.Name
                        : null,
                    InsuranceRx = i.Insurance != null ? i.Insurance.RxGroup : null,
                    BinCode = i.Insurance != null && i.Insurance.InsurancePCN != null && i.Insurance.InsurancePCN.Insurance != null
                        ? i.Insurance.InsurancePCN.Insurance.Bin
                        : null,
                    PcnCode = i.Insurance != null && i.Insurance.InsurancePCN != null
                        ? i.Insurance.InsurancePCN.PCN
                        : null,

                    // Prescriber (ScriptItem.Prescriber is a User, joined via UserEmail == User.Email)
                    PrescriberId = i.Prescriber != null ? i.Prescriber.Id : (int?)null,
                    PrescriberName = i.Prescriber != null ? i.Prescriber.Name : null,

                    // Branch (owned by the parent Script)
                    BranchId = i.Script.BranchId,
                    BranchCode = i.Script.Branch != null ? i.Script.Branch.Code : null,
                    BranchName = i.Script.Branch != null ? i.Script.Branch.Name : null,

                    // User (script owner - dispensing tech/pharmacist, owned by the parent Script)
                    UserId = i.Script.UserId,
                    UserName = i.Script.User != null ? i.Script.User.Name : null,

                    // Financial (raw)
                    Quantity = i.Quantity,
                    InsurancePayment = i.InsurancePayment,
                    PatientPayment = i.PatientPayment,
                    AcquisitionCost = i.AcquisitionCost,

                    // Financial (calculated in projection so it translates to SQL)
                    TotalRevenue = i.InsurancePayment + i.PatientPayment,
                    NetProfit = (i.InsurancePayment + i.PatientPayment) - i.AcquisitionCost,
                    NetProfitPerItem = i.Quantity > 0
                        ? ((i.InsurancePayment + i.PatientPayment) - i.AcquisitionCost) / i.Quantity
                        : (decimal?)null,

                    // Workflow
                    Status = i.Status,
                    RxStatus = i.RxStatus,
                    Priority = i.Priority,

                    // Medisearch fields — populated in a later sprint
                    HighestDrugNdc = null,
                    HighestDrugName = null,
                    HighestNet = null,
                    Difference = null
                })
                .ToListAsync(ct);

            return result;
        }
    }
}

