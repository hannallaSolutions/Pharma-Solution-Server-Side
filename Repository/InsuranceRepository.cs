using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SearchTool_ServerSide.Controllers;
using SearchTool_ServerSide.Data;
using SearchTool_ServerSide.Dtos.InsuranceDtos.cs;
using SearchTool_ServerSide.Models;

namespace SearchTool_ServerSide.Repository
{
    public class InsuranceRepository : GenericRepository<Insurance>
    {
        private readonly SearchToolDBContext _context;
        private readonly IMapper _mapper;
        public InsuranceRepository(SearchToolDBContext context, IMapper mapper) : base(context)
        {
            _context = context;
            _mapper = mapper;
        }
        internal async Task<InsuranceRx> GetInsuranceByName(string name)
        {
            var item = await _context.InsuranceRxes.Where(x => x.RxGroup == name).FirstOrDefaultAsync();
            return item;
        }
        internal async Task<InsuranceReadDto?> GetInsuranceDetails(int id)
        {
            var item = await (from rx in _context.InsuranceRxes
                              join pcn in _context.InsurancePCNs on rx.InsurancePCNId equals pcn.Id
                              join ins in _context.Insurances on pcn.InsuranceId equals ins.Id
                              where rx.Id == id
                              select new InsuranceReadDto
                              {
                                  RxGroup = rx.RxGroup,
                                  InsurancePCN = pcn.PCN,
                                  InsuranceBin = ins.Bin, // Assuming BIN exists in InsuranceRxes
                                  HelpDeskNumber = ins.HelpDeskNumber,
                                  InsuranceFullName = ins.Name
                              }).FirstOrDefaultAsync(); // Get a single result

            return item; // Return DTO or null if not found
        }
        internal async Task<InsuranceReadDto?> GetInsurancePCNDetails(int id)
        {
            var item = await (from pcn in _context.InsurancePCNs
                              join rx in _context.InsuranceRxes on pcn.Id equals rx.InsurancePCNId
                              join ins in _context.Insurances on pcn.InsuranceId equals ins.Id
                              where pcn.Id == id
                              select new InsuranceReadDto
                              {
                                  RxGroup = rx.RxGroup,
                                  InsurancePCN = pcn.PCN,
                                  InsuranceBin = ins.Bin, // Assuming BIN exists in InsuranceRxes
                                  HelpDeskNumber = ins.HelpDeskNumber,
                                  InsuranceFullName = ins.Name
                              }).FirstOrDefaultAsync(); // Get a single result

            return item; // Return DTO or null if not found
        }
        internal async Task<ICollection<InsuranceRx>> GetAllRxGroups()
        {
            var items = await _context.InsuranceRxes.ToListAsync();
            return items;
        }
        internal async Task<ICollection<InsuranceRx>> GetAllRxGroupsByPcnId(int id)
        {
            var items = await _context.InsuranceRxes.Where(x => x.InsurancePCNId == id).ToListAsync();
            return items;
        }
        internal async Task<ICollection<InsurancePCN>> GetAllPCNByBinId(int id)
        {
            var items = await _context.InsurancePCNs.Where(x => x.InsuranceId == id).ToListAsync();
            return items;
        }
        internal async Task<ICollection<Insurance>> GetAllBIN()
        {
            var items = await _context.Insurances.ToListAsync();
            return items;
        }

        internal async Task<Insurance> GetInsuranceBINDetails(int id)
        {
            return await _context.Insurances.FirstOrDefaultAsync(x => x.Id == id);
        }

        internal async Task<ICollection<InsuranceRx>> GetAllRxGroupsByBINId(int id)
        {
            var items = await _context.InsuranceRxes.Where(x => x.InsurancePCN.Insurance.Id == id).ToListAsync();
            return items;
        }

        internal async Task<ICollection<InsurancePCN>> GetAllPCNsByBINId(int id)
        {
            var items = await _context.InsurancePCNs.Where(x => x.Insurance.Id == id).ToListAsync();
            return items;
        }
        internal async Task<InsuranceRx> GetRXById(int id)
        {
            return await _context.InsuranceRxes.Include(x => x.InsurancePCN).ThenInclude(x => x.Insurance).FirstOrDefaultAsync(x => x.Id == id);
        }
        internal async Task<InsurancePCN> GetPCNById(int id)
        {
            return await _context.InsurancePCNs.FirstOrDefaultAsync(x => x.Id == id);
        }

        internal async Task ReportStatus(ReportStatusRequest request, string userEmail, CancellationToken ct = default)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));

            // Normalize incoming status
            var inputStatus = request.Status?.Trim();

            // 1) Ensure the parent InsuranceStatus exists (composite PK)
            var status = await _context.InsuranceStatuses.FindAsync(
                new object[] { request.SourceDrugNDC, request.TargetDrugNDC, request.InsuranceRxId }, ct);

            if (status == null)
            {
                _context.InsuranceStatuses.Add(new InsuranceStatus
                {
                    SourceDrugNDC = request.SourceDrugNDC,
                    TargetDrugNDC = request.TargetDrugNDC,
                    InsuranceRxId = request.InsuranceRxId,
                    ApprovedStatus = "NA",
                    PriorAuthorizationStatus = "NA"
                });

                try
                {
                    await _context.SaveChangesAsync(ct);
                    // Try to fetch the status again after insert
                    status = await _context.InsuranceStatuses.FindAsync(
                        new object[] { request.SourceDrugNDC, request.TargetDrugNDC, request.InsuranceRxId }, ct);
                }
                catch (DbUpdateException)
                {
                    // Another process may have inserted it; try to fetch again
                    status = await _context.InsuranceStatuses.FindAsync(
                        new object[] { request.SourceDrugNDC, request.TargetDrugNDC, request.InsuranceRxId }, ct);
                }
            }

            // Only update if status is not null
            if (status != null)
            {
                // Approved/Rejected mapping
                status.ApprovedStatus = inputStatus switch
                {
                    "Approved" => "Approved",
                    "Rejected" => "Rejected",
                    _ => status.ApprovedStatus ?? "NA"
                };

                // PA mapping (adds "Refile" alongside Yes/No)
                status.PriorAuthorizationStatus = inputStatus switch
                {
                    "PriorAuthorizationYes" => "Yes",
                    "PriorAuthorizationNo" => "No",
                    "PriorAuthorizationRefile" => "Refile",
                    _ => status.PriorAuthorizationStatus ?? "NA"
                };
            }

            _context.Reports.Add(new Report
            {
                SourceDrugNDC = request.SourceDrugNDC,
                TargetDrugNDC = request.TargetDrugNDC,
                InsuranceRxId = request.InsuranceRxId,

                Status = string.IsNullOrWhiteSpace(inputStatus) ? "NA" : inputStatus,
                StatusDescription = "NA",
                AdditionalInfo = "NA",
                StatusDate = DateTime.UtcNow,

                UserEmail = string.IsNullOrWhiteSpace(userEmail) ? null : userEmail
            });

            await _context.SaveChangesAsync(ct);
        }

        internal async Task<IEnumerable<Report>> GetReportsAsyncByKey(string sourceDrugNDC, string TargetDrugNDC, int insuranceRxId, CancellationToken ct = default, int pageSize = 3)
        {
            return await _context.Reports
                .Where(r => r.SourceDrugNDC == sourceDrugNDC && r.TargetDrugNDC == TargetDrugNDC && r.InsuranceRxId == insuranceRxId)
                .OrderByDescending(r => r.StatusDate)
                .Skip(0)
                .Take(pageSize)
                .ToListAsync(ct);
        }
        internal async Task<IEnumerable<Report>> GetReportsAsyncByTargetNDC(string targetDrugNDC, int insuranceRxId, CancellationToken ct = default, int pageSize = 3)
        {
            return await _context.Reports
                .Where(r => r.TargetDrugNDC == targetDrugNDC && r.InsuranceRxId == insuranceRxId)
                .OrderByDescending(r => r.StatusDate)
                .Skip(0)
                .Take(pageSize)
                .ToListAsync(ct);
        }
        internal async Task<bool> CheckInsuranceAvailability(CustomAddDrugInsuranceRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // 1) BIN
            Insurance insuranceBIN = null;
            if (!string.IsNullOrEmpty(request.InsuranceBin))
            {
                insuranceBIN = await _context.Insurances
                    .FirstOrDefaultAsync(x => x.Name == request.InsuranceBin);

                Console.WriteLine("insuranceBIN: " + (insuranceBIN?.Id.ToString() ?? "null"));

                // if BIN in request but not found in DB
                if (insuranceBIN == null)
                    return true;
            }

            // 2) PCN
            InsurancePCN insurancePCN = null;
            if (!string.IsNullOrEmpty(request.InsurancePCN))
            {
                // can't search PCN if BIN not found
                if (insuranceBIN == null)
                    return true;

                insurancePCN = await _context.InsurancePCNs
                    .FirstOrDefaultAsync(x =>
                        x.PCN == request.InsurancePCN &&
                        x.InsuranceId == insuranceBIN.Id);

                Console.WriteLine("insurancePCN: " + (insurancePCN?.Id.ToString() ?? "null"));

                if (insurancePCN == null)
                    return true;
            }

            // 3) RxGroup
            InsuranceRx insuranceRX = null;
            if (!string.IsNullOrEmpty(request.InsuranceRx))
            {
                if (insurancePCN == null)
                    return true;

                insuranceRX = await _context.InsuranceRxes
                    .FirstOrDefaultAsync(x =>
                        x.RxGroup == request.InsuranceRx &&
                        x.InsurancePCNId == insurancePCN.Id);

                Console.WriteLine("insuranceRX: " + (insuranceRX?.Id.ToString() ?? "null"));

                if (insuranceRX == null)
                    return true;
            }

            // 4) DrugInsurance
            if (insuranceRX == null)
                return true;

            var drugInsurance = await _context.DrugInsurances
                .FirstOrDefaultAsync(x =>
                    x.DrugId == request.DrugId &&
                    x.InsuranceId == insuranceRX.Id);

            Console.WriteLine(
                "drugId: " + request.DrugId +
                " insuranceRX.Id: " + insuranceRX.Id +
                " drugInsurance: " + (drugInsurance?.DrugId.ToString() ?? "null")
            );

            if (drugInsurance == null)
                return true;

            // if everything exists -> not available to add (based on your current logic)
            return false;
        }

        internal async Task HandleCustomAddDrugInsurance(CustomAddDrugInsuranceRequest request, CancellationToken ct = default, int branchId = 1, string userEmail = "")
        {
            var insuranceBIN = await _context.Insurances.FirstOrDefaultAsync(x => x.Name == request.InsuranceBin, ct);
            if (request.InsuranceBin != null && insuranceBIN == null)
            {
                insuranceBIN = new Insurance
                {
                    Name = request.InsuranceBin,
                    Bin = request.InsuranceBinCode ?? request.InsuranceBin,
                    HelpDeskNumber = "NA"

                };
                _context.Insurances.Add(insuranceBIN);
                await _context.SaveChangesAsync(ct);
            }
            var insurancePCN = await _context.InsurancePCNs.FirstOrDefaultAsync(x => x.PCN == request.InsurancePCN && x.InsuranceId == insuranceBIN.Id, ct);
            if (request.InsurancePCN != null && insurancePCN == null)
            {
                insurancePCN = new InsurancePCN
                {
                    PCN = request.InsurancePCN,
                    InsuranceId = insuranceBIN.Id

                };
                _context.InsurancePCNs.Add(insurancePCN);
                await _context.SaveChangesAsync(ct);
            }
            var insuranceRX = await _context.InsuranceRxes.FirstOrDefaultAsync(x => x.RxGroup == request.InsuranceRx && x.InsurancePCNId == insurancePCN.Id, ct);
            if (request.InsuranceRx != null && insuranceRX == null)
            {
                insuranceRX = new InsuranceRx
                {
                    RxGroup = request.InsuranceRx,
                    InsurancePCNId = insurancePCN.Id

                };
                _context.InsuranceRxes.Add(insuranceRX);
                await _context.SaveChangesAsync(ct);
            }
            var drug = await _context.Drugs.FirstOrDefaultAsync(x => x.Id == request.DrugId, ct);
            var drugInsurance = await _context.DrugInsurances.FirstOrDefaultAsync(x => x.DrugId == request.DrugId && x.InsuranceId == insuranceRX.Id, ct);
            if (drugInsurance == null)
            {
                drugInsurance = new DrugInsurance
                {
                    DrugId = request.DrugId,
                    InsuranceId = insuranceRX.Id,
                    Date = DateTime.UtcNow,
                    NDCCode = drug.NDC,
                    BranchId = branchId,
                    Quantity = 1,
                    AcquisitionCost = 0,
                    PatientPayment = 0,
                    InsurancePayment = 0,
                    Net = 0,
                    Prescriber = "NA",

                };
                _context.DrugInsurances.Add(drugInsurance);
                await _context.SaveChangesAsync(ct);
                var log = new Log
                {
                    Action = $"Processing custom insurance addition for DrugId: {request.DrugId}, InsuranceRx: {request.InsuranceRx}, InsurancePCN: {request.InsurancePCN}, InsuranceBin: {request.InsuranceBin} at branch {branchId}",
                    Date = DateTime.UtcNow,
                    UserEmail = userEmail
                };
                _context.Logs.Add(log);
                await _context.SaveChangesAsync(ct);
            }
        }


    }
}