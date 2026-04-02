using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SearchTool_ServerSide.Controllers;
using SearchTool_ServerSide.Data;
using SearchTool_ServerSide.Models;

namespace SearchTool_ServerSide.Repository
{
    public class DrugClassRepository : GenericRepository<DrugClass>
    {
        private readonly SearchToolDBContext _context;
        private readonly IMapper _mapper;
        public DrugClassRepository(SearchToolDBContext context, IMapper mapper) : base(context)
        {
            _context = context;
            _mapper = mapper;
        }
        internal async Task ReportStatus(DrugClassReportStatusRequest request, string userEmail, CancellationToken ct = default)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));

            // Normalize incoming status
            var inputStatus = request.Status?.Trim();

            // 1) Ensure the parent DrugAlternativeStatus exists (composite PK)
            var status = await _context.DrugAlternativeStatuses.FindAsync(
                new object[] { request.SourceDrugNDC, request.TargetDrugNDC, request.ClassInfoId }, ct);

            if (status == null)
            {
                _context.DrugAlternativeStatuses.Add(new DrugAlternativeStatus
                {
                    SourceDrugNDC = request.SourceDrugNDC,
                    TargetDrugNDC = request.TargetDrugNDC,
                    ClassInfoId = request.ClassInfoId,
                    ApprovedStatus = "NA",
                });

                try
                {
                    await _context.SaveChangesAsync(ct);
                    // Try to fetch the status again after insert
                    status = await _context.DrugAlternativeStatuses.FindAsync(
                        new object[] { request.SourceDrugNDC, request.TargetDrugNDC, request.ClassInfoId }, ct);
                }
                catch (DbUpdateException)
                {
                    // Another process may have inserted it; try to fetch again
                    status = await _context.DrugAlternativeStatuses.FindAsync(
                        new object[] { request.SourceDrugNDC, request.TargetDrugNDC, request.ClassInfoId }, ct);
                }
            }

            // Only update if status is not null
            if (status != null)
            {
                // Approved/Rejected mapping
                status.ApprovedStatus = inputStatus switch
                {
                    "Yes" => "Yes",
                    "No" => "No",
                    _ => status.ApprovedStatus ?? "NA"
                };

            }

            // 2) Append a new Report row (history)
            _context.DrugAlternativeReports.Add(new DrugAlternativeReport
            {
                SourceDrugNDC = request.SourceDrugNDC,
                TargetDrugNDC = request.TargetDrugNDC,
                ClassInfoId = request.ClassInfoId,

                Status = string.IsNullOrWhiteSpace(inputStatus) ? "NA" : inputStatus,
                StatusDescription = "NA",
                AdditionalInfo = "NA",
                StatusDate = DateTime.UtcNow,

                UserEmail = string.IsNullOrWhiteSpace(userEmail) ? null : userEmail
            });

            await _context.SaveChangesAsync(ct);
        }

        internal async Task<IEnumerable<DrugAlternativeReport>> GetReportsAsyncByKey(string sourceDrugNDC, string targetDrugNDC, int classInfoId, CancellationToken ct = default, int pageSize = 3)
        {
            return await _context.DrugAlternativeReports
                .Where(r => r.SourceDrugNDC == sourceDrugNDC && r.TargetDrugNDC == targetDrugNDC && r.ClassInfoId == classInfoId)
                .OrderByDescending(r => r.StatusDate)
                .Skip(0)
                .Take(pageSize)
                .ToListAsync(ct);
        }
    }
}