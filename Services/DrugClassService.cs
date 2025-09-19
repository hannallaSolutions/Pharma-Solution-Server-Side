using AutoMapper;
using SearchTool_ServerSide.Controllers;
using SearchTool_ServerSide.Models;
using SearchTool_ServerSide.Repository;

namespace SearchTool_ServerSide.Services
{
    public class DrugClassService
    {
        private readonly DrugClassRepository _drugClassRepository;
        private readonly IMapper _mapper;

        public DrugClassService(DrugClassRepository drugClassRepository, IMapper mapper)
        {
            _drugClassRepository = drugClassRepository;
            _mapper = mapper;
        }

        public async Task ReportStatus(DrugClassReportStatusRequest request, string userEmail, CancellationToken ct = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(userEmail)) throw new ArgumentNullException(nameof(userEmail));

            await _drugClassRepository.ReportStatus(request, userEmail, ct);
        }
        public async Task<IEnumerable<DrugAlternativeReport>> GetReportsAsyncByKey(string sourceDrugNDC, string targetDrugNDC, int classInfoId, CancellationToken ct = default, int pageSize = 3)
        {
            if (string.IsNullOrWhiteSpace(sourceDrugNDC)) throw new ArgumentNullException(nameof(sourceDrugNDC));
            if (string.IsNullOrWhiteSpace(targetDrugNDC)) throw new ArgumentNullException(nameof(targetDrugNDC));
            if (classInfoId <= 0) throw new ArgumentOutOfRangeException(nameof(classInfoId), "ClassInfoId must be greater than zero.");

            return await _drugClassRepository.GetReportsAsyncByKey(sourceDrugNDC, targetDrugNDC, classInfoId, ct, pageSize);
        }
    }
}