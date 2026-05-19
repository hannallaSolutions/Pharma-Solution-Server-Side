using SearchTool_ServerSide.Dtos.DrugWholesalerPrescriberDtos;
using SearchTool_ServerSide.Models;
using SearchTool_ServerSide.Repository;

namespace SearchTool_ServerSide.Services
{
    public class DrugWholesalerPrescriberService
    {
        private readonly DrugWholesalerPrescriberRepository _repository;

        public DrugWholesalerPrescriberService(
            DrugWholesalerPrescriberRepository repository)
        {
            _repository = repository;
        }
        public async Task<UserInsuranceContract> AddContractAsync(AddUserInsuranceContractRequest request)
        {
            try
            {
                var contract = await _repository.AddContractAsync(request);
                return contract;
            }
            catch (ArgumentException ex)
            {
                throw new ArgumentException("error at wholesaler", ex);    
            }
        }
        public async Task<WholesalerImportResultDto> ImportPricesFileAsync(
            IFormFile file,
            int defaultPrescriberId,
            CancellationToken ct = default)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Uploaded file is empty.");

            if (defaultPrescriberId <= 0)
                throw new ArgumentException("PrescriberId is required.");

            return await _repository.ImportPricesFileAsync(
                file,
                defaultPrescriberId,
                ct);
        }

        public async Task<DrugWholesalerPrescriber> AddSingleAsync(
            int drugId,
            int wholesalerId,
            int prescriberId,
            decimal price,
            DateTime priceDate,
            decimal? awp = null,
            decimal? wac = null,
            decimal? asp = null,
            decimal? mac = null,
            string? billingUnit = null,
            string? drugClass = null,
            string? quarterYear = null,
            string? sourceFileName = null,
            string? sourcePath = null,
            CancellationToken ct = default)
        {
            if (drugId <= 0)
                throw new ArgumentException("DrugId is required.");

            if (wholesalerId <= 0)
                throw new ArgumentException("WholesalerId is required.");

            if (prescriberId <= 0)
                throw new ArgumentException("PrescriberId is required.");

            if (price <= 0)
                throw new ArgumentException("Price must be greater than zero.");

            if (priceDate == default)
                throw new ArgumentException("PriceDate is required.");

            return await _repository.AddSingleAsync(
                drugId,
                wholesalerId,
                prescriberId,
                price,
                priceDate,
                awp,
                wac,
                asp,
                mac,
                billingUnit,
                drugClass,
                quarterYear,
                sourceFileName,
                sourcePath,
                ct);
        }

        public async Task<List<DrugWholesalerPrescriber>> GetLatestPricesForDrugAsync(
            int drugId,
            int prescriberId,
            CancellationToken ct = default)
        {
            if (drugId <= 0)
                throw new ArgumentException("DrugId is required.");

            if (prescriberId <= 0)
                throw new ArgumentException("PrescriberId is required.");

            return await _repository.GetLatestPricesForDrugAsync(
                drugId,
                prescriberId,
                ct);
        }

        public async Task<DrugWholesalerPrescriber?> GetBestPriceAsync(
            int drugId,
            int prescriberId,
            CancellationToken ct = default)
        {
            if (drugId <= 0)
                throw new ArgumentException("DrugId is required.");

            if (prescriberId <= 0)
                throw new ArgumentException("PrescriberId is required.");

            return await _repository.GetBestPriceAsync(
                drugId,
                prescriberId,
                ct);
        }
    

    public async Task<List<DrugWholesalerPrescriber>> GetAllPricesForPrescriberAsync(
    int prescriberId,
    CancellationToken ct = default)
{
    if (prescriberId <= 0)
        throw new ArgumentException("Prescriber ID is required.");

    return await _repository.GetAllPricesForPrescriberAsync(prescriberId, ct);
}


//get all prescribers for dropdown
    public async Task<List<PrescriberOptionDto>> GetAllPrescribersAsync(
        CancellationToken ct = default)
        {
            return await _repository.GetAllPrescribersAsync(ct);
            
            
    }

        internal async Task GetPrescriberOptionsAsync(CancellationToken ct)
        {
            throw new NotImplementedException();

        }

        public async Task<ReimbursementParametersDto?> GetReimbursementParametersAsync(
            int userId,
            int insuranceRxId,
            CancellationToken ct = default)
        {
            var result = await _repository.GetReimbursementParametersAsync(userId, insuranceRxId);

            return result;
        }
    }

}