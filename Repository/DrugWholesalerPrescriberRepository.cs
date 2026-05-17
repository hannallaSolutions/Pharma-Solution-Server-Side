using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using SearchTool_ServerSide.Data;
using SearchTool_ServerSide.Models;

namespace SearchTool_ServerSide.Repository
{
    public class DrugWholesalerPrescriberRepository(SearchToolDBContext _context)
    {
        // =====================================================
        // 1. Main entry point: accepts Excel or CSV
        // =====================================================
        public async Task<WholesalerImportResultDto> ImportPricesFileAsync(
            IFormFile file,
            int defaultPrescriberId,
            CancellationToken ct = default)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Uploaded file is empty.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            return extension switch
            {
                ".xlsx" => await ImportFromExcelAsync(file, defaultPrescriberId, ct),
                ".csv" => await ImportFromCsvAsync(file, defaultPrescriberId, ct),
                _ => throw new ArgumentException("Only .xlsx and .csv files are supported.")
            };
        }

        // =====================================================
        // 2. Add one price manually
        // =====================================================
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
            var cleanDate = priceDate.Date;

            var existingSameRecord = await _context.DrugWholesalerPrescribers
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.DrugId == drugId &&
                    x.WholesalerId == wholesalerId &&
                    x.PrescriberId == prescriberId &&
                    x.PriceDate.Date == cleanDate &&
                    x.Price == price,
                    ct);

            if (existingSameRecord != null)
                return existingSameRecord;

            var entity = new DrugWholesalerPrescriber
            {
                DrugId = drugId,
                WholesalerId = wholesalerId,
                PrescriberId = prescriberId,

                Price = price,
                PriceDate = cleanDate,

                AWP = awp,
                WAC = wac,
                ASP = asp,
                MAC = mac,

                BillingUnit = billingUnit,
                DrugClass = drugClass,
                QuarterYear = quarterYear,

                SourceFileName = sourceFileName,
                SourcePath = sourcePath,

                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.DrugWholesalerPrescribers.AddAsync(entity, ct);
            await _context.SaveChangesAsync(ct);

            return entity;
        }

        // =====================================================
        // 3. Import from Excel
        // Expected columns:
        // DrugName, NDC, DrugClass, AWP, WAC, ASP, MAC,
        // BillingUnit, CuraScriptPrice, BesseCencoraPrice,
        // MorrisDicksonPrice, QuarterYear, PriceDate, Path
        // =====================================================
        public async Task<WholesalerImportResultDto> ImportFromExcelAsync(
            IFormFile file,
            int defaultPrescriberId,
            CancellationToken ct = default)
        {
            var result = new WholesalerImportResultDto
            {
                FileName = file.FileName
            };

            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);

            var worksheet = workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
            {
                result.Errors.Add("No worksheet found in Excel file.");
                return result;
            }

            var usedRange = worksheet.RangeUsed();

            if (usedRange == null)
            {
                result.Errors.Add("Excel file is empty.");
                return result;
            }

            var rows = usedRange.RowsUsed().Skip(1).ToList();

            if (!rows.Any())
                return result;

            var lookup = await BuildImportLookupAsync(ct);

            var newRows = new List<DrugWholesalerPrescriber>();

            foreach (var row in rows)
            {
                result.TotalRows++;

                var excelRowNumber = row.RowNumber();

                try
                {
                    var record = new WholesalerPriceFileRowDto
                    {
                        DrugName = GetCell(row, 1),
                        NDC = GetCell(row, 2),
                        DrugClass = GetCell(row, 3),

                        AWP = GetCell(row, 4),
                        WAC = GetCell(row, 5),
                        ASP = GetCell(row, 6),
                        MAC = GetCell(row, 7),

                        BillingUnit = GetCell(row, 8),

                        CuraScriptPrice = GetCell(row, 9),
                        BesseCencoraPrice = GetCell(row, 10),
                        MorrisDicksonPrice = GetCell(row, 11),

                        QuarterYear = GetCell(row, 12),
                        PriceDate = GetCell(row, 13),
                        Path = GetCell(row, 14)
                    };

                    await ProcessImportRecordAsync(
                        record,
                        file.FileName,
                        defaultPrescriberId,
                        lookup,
                        newRows,
                        result,
                        excelRowNumber,
                        ct);
                }
                catch (Exception ex)
                {
                    result.FailedRows++;
                    result.Errors.Add($"Row {excelRowNumber}: {ex.Message}");
                }
            }

            if (newRows.Any())
            {
                await _context.DrugWholesalerPrescribers.AddRangeAsync(newRows, ct);
                await _context.SaveChangesAsync(ct);
            }

            result.ImportedRows = newRows.Count;

            return result;
        }

        // =====================================================
        // 4. Import from CSV
        // Expected headers:
        // DrugName,NDC,DrugClass,AWP,WAC,ASP,MAC,BillingUnit,
        // CuraScriptPrice,BesseCencoraPrice,MorrisDicksonPrice,
        // QuarterYear,PriceDate,Path
        // =====================================================
        public async Task<WholesalerImportResultDto> ImportFromCsvAsync(
            IFormFile file,
            int defaultPrescriberId,
            CancellationToken ct = default)
        {
            var result = new WholesalerImportResultDto
            {
                FileName = file.FileName
            };

            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                HeaderValidated = null,
                MissingFieldFound = null,
                BadDataFound = null,
                TrimOptions = TrimOptions.Trim
            };

            using var csv = new CsvReader(reader, config);
            var records = csv.GetRecords<WholesalerPriceFileRowDto>().ToList();

            var lookup = await BuildImportLookupAsync(ct);

            var newRows = new List<DrugWholesalerPrescriber>();

            var rowNumber = 1; // header row

            foreach (var record in records)
            {
                rowNumber++;
                result.TotalRows++;

                try
                {
                    await ProcessImportRecordAsync(
                        record,
                        file.FileName,
                        defaultPrescriberId,
                        lookup,
                        newRows,
                        result,
                        rowNumber,
                        ct);
                }
                catch (Exception ex)
                {
                    result.FailedRows++;
                    result.Errors.Add($"Row {rowNumber}: {ex.Message}");
                }
            }

            if (newRows.Any())
            {
                await _context.DrugWholesalerPrescribers.AddRangeAsync(newRows, ct);
                await _context.SaveChangesAsync(ct);
            }

            result.ImportedRows = newRows.Count;

            return result;
        }

        // =====================================================
        // 5. Get latest prices for one drug and prescriber
        // =====================================================
        public async Task<List<DrugWholesalerPrescriber>> GetLatestPricesForDrugAsync(
            int drugId,
            int prescriberId,
            CancellationToken ct = default)
        {
            var allPrices = await _context.DrugWholesalerPrescribers
                .Include(x => x.Drug)
                .Include(x => x.Wholesaler)
                .Include(x => x.Prescriber)
                .Where(x =>
                    x.DrugId == drugId &&
                    x.PrescriberId == prescriberId &&
                    x.IsActive)
                .ToListAsync(ct);

            return allPrices
                .GroupBy(x => x.WholesalerId)
                .Select(g => g
                    .OrderByDescending(x => x.PriceDate)
                    .ThenByDescending(x => x.CreatedAt)
                    .First())
                .OrderBy(x => x.Price)
                .ToList();
        }

        // =====================================================
        // 6. Get best current price for one drug and prescriber
        // =====================================================
        public async Task<DrugWholesalerPrescriber?> GetBestPriceAsync(
            int drugId,
            int prescriberId,
            CancellationToken ct = default)
        {
            var latestPrices = await GetLatestPricesForDrugAsync(drugId, prescriberId, ct);

            return latestPrices
                .OrderBy(x => x.Price)
                .FirstOrDefault();
        }

        // =====================================================
        // 7. Process one row from Excel or CSV
        // =====================================================
        private async Task ProcessImportRecordAsync(
            WholesalerPriceFileRowDto record,
            string fileName,
            int defaultPrescriberId,
            ImportLookupData lookup,
            List<DrugWholesalerPrescriber> newRows,
            WholesalerImportResultDto result,
            int rowNumber,
            CancellationToken ct)
        {
            var ndc = NormalizeNdc(record.NDC);

            if (string.IsNullOrWhiteSpace(ndc))
            {
                result.SkippedRows++;
                result.Errors.Add($"Row {rowNumber}: Missing NDC.");
                return;
            }

            if (!lookup.DrugByNdc.TryGetValue(ndc, out var drug))
            {
                result.SkippedRows++;
                result.Errors.Add($"Row {rowNumber}: Drug not found for NDC {ndc}.");
                return;
            }

            var priceDate = ParseDate(record.PriceDate) ?? DateTime.UtcNow.Date;

            var awp = ParseDecimal(record.AWP);
            var wac = ParseDecimal(record.WAC);
            var asp = ParseDecimal(record.ASP);
            var mac = ParseDecimal(record.MAC);

            var priceMap = new Dictionary<string, decimal?>
            {
                { "CuraScript", ParseDecimal(record.CuraScriptPrice) },
                { "Besse/Cencora", ParseDecimal(record.BesseCencoraPrice) },
                { "Morris & Dickson", ParseDecimal(record.MorrisDicksonPrice) }
            };

            var hasAtLeastOnePrice = false;

            foreach (var item in priceMap)
            {
                var wholesalerName = item.Key;
                var price = item.Value;

                if (price == null || price <= 0)
                    continue;

                hasAtLeastOnePrice = true;

                var wholesaler = await GetOrCreateWholesalerAsync(
                    wholesalerName,
                    lookup.WholesalerByName,
                    ct);

                var key = BuildKey(
                    drug.Id,
                    wholesaler.Id,
                    defaultPrescriberId,
                    priceDate,
                    price.Value);

                if (lookup.ExistingKeys.Contains(key))
                {
                    result.DuplicateRows++;
                    continue;
                }

                var entity = new DrugWholesalerPrescriber
                {
                    DrugId = drug.Id,
                    WholesalerId = wholesaler.Id,
                    PrescriberId = defaultPrescriberId,

                    Price = price.Value,
                    PriceDate = priceDate,

                    AWP = awp,
                    WAC = wac,
                    ASP = asp,
                    MAC = mac,

                    BillingUnit = record.BillingUnit,
                    DrugClass = record.DrugClass,
                    QuarterYear = record.QuarterYear,

                    SourceFileName = fileName,
                    SourcePath = record.Path,

                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                newRows.Add(entity);
                lookup.ExistingKeys.Add(key);
            }

            if (!hasAtLeastOnePrice)
            {
                result.SkippedRows++;
                result.Errors.Add($"Row {rowNumber}: No valid wholesaler price found.");
            }
        }

        // =====================================================
        // 8. Build lookup data for faster import
        // =====================================================
        private async Task<ImportLookupData> BuildImportLookupAsync(CancellationToken ct)
        {
            var drugs = await _context.Drugs
                .AsNoTracking()
                .Where(d => d.NDC != null)
                .ToListAsync(ct);

            var drugByNdc = drugs
                .GroupBy(d => NormalizeNdc(d.NDC))
                .ToDictionary(g => g.Key, g => g.First());

            var wholesalers = await _context.Wholesalers
                .ToListAsync(ct);

            var wholesalerByName = wholesalers
                .GroupBy(w => NormalizeText(w.Name))
                .ToDictionary(g => g.Key, g => g.First());

            var existingPrices = await _context.DrugWholesalerPrescribers
                .AsNoTracking()
                .Select(x => new
                {
                    x.DrugId,
                    x.WholesalerId,
                    x.PrescriberId,
                    x.PriceDate,
                    x.Price
                })
                .ToListAsync(ct);

            var existingKeys = existingPrices
                .Select(x => BuildKey(
                    x.DrugId,
                    x.WholesalerId,
                    x.PrescriberId,
                    x.PriceDate,
                    x.Price))
                .ToHashSet();

            return new ImportLookupData
            {
                DrugByNdc = drugByNdc,
                WholesalerByName = wholesalerByName,
                ExistingKeys = existingKeys
            };
        }

        // =====================================================
        // 9. Get or create wholesaler
        // =====================================================
        private async Task<Wholesaler> GetOrCreateWholesalerAsync(
            string name,
            Dictionary<string, Wholesaler> wholesalerByName,
            CancellationToken ct)
        {
            var key = NormalizeText(name);

            if (wholesalerByName.TryGetValue(key, out var existing))
                return existing;

            var wholesaler = new Wholesaler
            {
                Name = name.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Wholesalers.AddAsync(wholesaler, ct);
            await _context.SaveChangesAsync(ct);

            wholesalerByName[key] = wholesaler;

            return wholesaler;
        }

        // =====================================================
        // 10. Helpers
        // =====================================================
        private static string GetCell(IXLRangeRow row, int index)
        {
            return row.Cell(index).GetString()?.Trim() ?? string.Empty;
        }

        private static string NormalizeText(string? value)
        {
            return (value ?? string.Empty)
                .Trim()
                .ToUpperInvariant();
        }

        private static string NormalizeNdc(string? value)
        {
            var digits = new string((value ?? string.Empty)
                .Where(char.IsDigit)
                .ToArray());

            if (string.IsNullOrWhiteSpace(digits))
                return string.Empty;

            if (digits.Length < 11)
                digits = digits.PadLeft(11, '0');

            return digits;
        }

        private static decimal? ParseDecimal(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var cleaned = value
                .Replace("$", "")
                .Replace(",", "")
                .Trim();

            if (decimal.TryParse(
                    cleaned,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out var result))
            {
                return result;
            }

            return null;
        }

        private static DateTime? ParseDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (DateTime.TryParse(
                    value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var date))
            {
                return date.Date;
            }

            return null;
        }

        private static string BuildKey(
            int drugId,
            int wholesalerId,
            int prescriberId,
            DateTime priceDate,
            decimal price)
        {
            return $"{drugId}|{wholesalerId}|{prescriberId}|{priceDate:yyyy-MM-dd}|{price}";
        }
        public async Task<ReimbursementParametersDto?> GetReimbursementParametersAsync(
            int userId,
            int insuranceRxId,
            CancellationToken ct = default)
        {
            var contract = await _context.UserInsuranceContracts
                .Include(c => c.InsuranceRx)
                .AsNoTracking()
                .Where(c => c.UserId == userId
                         && c.InsuranceRxId == insuranceRxId
                         && c.IsActive
                         && (c.EffectiveTo == null || c.EffectiveTo >= DateTime.UtcNow))
                .OrderByDescending(c => c.EffectiveFrom)
                .FirstOrDefaultAsync(ct);

            if (contract == null) return null;

            return new ReimbursementParametersDto
            {
                ContractId = contract.Id,
                InsurancePlanName = contract.InsuranceRx?.RxGroup ?? string.Empty,
                ReimbursementType = contract.ReimbursementType,
                AwpDiscountPercent = contract.AwpDiscountPercent,
                AspMarkupPercent = contract.AspMarkupPercent,
                MacPrice = contract.MacPrice,
                FixedReimbursementAmount = contract.FixedReimbursementAmount,
                DispensingFee = contract.DispensingFee,
                ExpectedPatientPay = contract.ExpectedPatientPay,
                EffectiveFrom = contract.EffectiveFrom,
                EffectiveTo = contract.EffectiveTo,
                Notes = contract.Notes
            };
        }
        public async Task<UserInsuranceContract> AddContractAsync(
    AddUserInsuranceContractRequest request,
    CancellationToken ct = default)
        {
            // Validate ReimbursementType has the required field
            ValidateContractFields(request);

            // Deactivate any existing active contract for same user + insurance
            var existing = await _context.UserInsuranceContracts
                .Where(c => c.UserId == request.UserId
                         && c.InsuranceRxId == request.InsuranceRxId
                         && c.IsActive)
                .ToListAsync(ct);

            foreach (var old in existing)
            {
                old.IsActive = false;
                old.UpdatedAt = DateTime.UtcNow;
            }

            var contract = new UserInsuranceContract
            {
                UserId = request.UserId,
                InsuranceRxId = request.InsuranceRxId,
                ReimbursementType = request.ReimbursementType.ToUpperInvariant().Trim(),
                AwpDiscountPercent = request.AwpDiscountPercent,
                AspMarkupPercent = request.AspMarkupPercent,
                MacPrice = request.MacPrice,
                FixedReimbursementAmount = request.FixedReimbursementAmount,
                DispensingFee = request.DispensingFee,
                ExpectedPatientPay = request.ExpectedPatientPay,
                EffectiveFrom = request.EffectiveFrom,
                EffectiveTo = request.EffectiveTo,
                Notes = request.Notes,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.UserInsuranceContracts.AddAsync(contract, ct);
            await _context.SaveChangesAsync(ct);

            return contract;
        }

        // =====================================================
        // Validation — each type requires its own field
        // =====================================================
        private static void ValidateContractFields(AddUserInsuranceContractRequest r)
        {
            var type = r.ReimbursementType?.ToUpperInvariant().Trim();

            switch (type)
            {
                case "AWP":
                    if (r.AwpDiscountPercent is null)
                        throw new ArgumentException("AWP contract requires AwpDiscountPercent.");
                    if (r.AwpDiscountPercent is < 0 or > 100)
                        throw new ArgumentException("AwpDiscountPercent must be between 0 and 100.");
                    break;

                case "ASP":
                    if (r.AspMarkupPercent is null)
                        throw new ArgumentException("ASP contract requires AspMarkupPercent.");
                    if (r.AspMarkupPercent < 0)
                        throw new ArgumentException("AspMarkupPercent cannot be negative.");
                    break;

                case "MAC":
                    if (r.MacPrice is null or <= 0)
                        throw new ArgumentException("MAC contract requires a valid MacPrice.");
                    break;

                case "FIXED":
                    if (r.FixedReimbursementAmount is null or <= 0)
                        throw new ArgumentException("FIXED contract requires FixedReimbursementAmount.");
                    break;

                case null or "":
                    throw new ArgumentException("ReimbursementType is required.");

                default:
                    throw new ArgumentException(
                        $"Invalid ReimbursementType '{r.ReimbursementType}'. " +
                        "Allowed values: AWP, ASP, MAC, FIXED.");
            }

            if (r.EffectiveFrom.HasValue && r.EffectiveTo.HasValue
                && r.EffectiveTo <= r.EffectiveFrom)
                throw new ArgumentException("EffectiveTo must be after EffectiveFrom.");
        }


        // =====================================================
        // Internal lookup object
        // =====================================================
        internal class ImportLookupData
        {
            public Dictionary<string, Drug> DrugByNdc { get; set; } = new();
            public Dictionary<string, Wholesaler> WholesalerByName { get; set; } = new();
            public HashSet<string> ExistingKeys { get; set; } = new();
        }
        
    }
    public class ReimbursementParametersDto
    {
        public int ContractId { get; set; }
        public string InsurancePlanName { get; set; } = string.Empty;
        public string ReimbursementType { get; set; } = string.Empty;

        // Only one of these will be populated depending on ReimbursementType
        public decimal? AwpDiscountPercent { get; set; }
        public decimal? AspMarkupPercent { get; set; }
        public decimal? MacPrice { get; set; }
        public decimal? FixedReimbursementAmount { get; set; }

        public decimal? DispensingFee { get; set; }
        public decimal? ExpectedPatientPay { get; set; }

        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public string? Notes { get; set; }
    }
    // =====================================================
    // DTO: File row structure for CSV and Excel
    // =====================================================
    public class WholesalerPriceFileRowDto
    {
        public string? DrugName { get; set; }
        public string? NDC { get; set; }
        public string? DrugClass { get; set; }

        public string? AWP { get; set; }
        public string? WAC { get; set; }
        public string? ASP { get; set; }
        public string? MAC { get; set; }

        public string? BillingUnit { get; set; }

        public string? CuraScriptPrice { get; set; }
        public string? BesseCencoraPrice { get; set; }
        public string? MorrisDicksonPrice { get; set; }

        public string? QuarterYear { get; set; }
        public string? PriceDate { get; set; }
        public string? Path { get; set; }
    }

    // =====================================================
    // DTO: Import result
    // =====================================================
    public class WholesalerImportResultDto
    {
        public string FileName { get; set; } = string.Empty;

        public int TotalRows { get; set; }
        public int ImportedRows { get; set; }
        public int SkippedRows { get; set; }
        public int FailedRows { get; set; }
        public int DuplicateRows { get; set; }

        public List<string> Errors { get; set; } = new();
    }
    // =====================================================
    // Request DTO
    // =====================================================
    public class AddUserInsuranceContractRequest
    {
        public int UserId { get; set; }
        public int InsuranceRxId { get; set; }

        // AWP / ASP / MAC / FIXED
        public string ReimbursementType { get; set; } = string.Empty;

        public decimal? AwpDiscountPercent { get; set; }
        public decimal? AspMarkupPercent { get; set; }
        public decimal? MacPrice { get; set; }
        public decimal? FixedReimbursementAmount { get; set; }

        public decimal? DispensingFee { get; set; }
        public decimal? ExpectedPatientPay { get; set; }

        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }

        public string? Notes { get; set; }
    }
}