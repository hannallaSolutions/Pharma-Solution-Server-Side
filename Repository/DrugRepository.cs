using AutoMapper;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using CsvHelper.TypeConversion;
using ExcelDataReader;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OfficeOpenXml;
using SearchTool_ServerSide.Data;
using SearchTool_ServerSide.Dtos;
using SearchTool_ServerSide.Dtos.BranchDTOs;
using SearchTool_ServerSide.Dtos.ClassDtos;
using SearchTool_ServerSide.Dtos.DrugDtos;
using SearchTool_ServerSide.Dtos.InsuranceDtos.cs;
using SearchTool_ServerSide.Dtos.ScritpsDto;
using SearchTool_ServerSide.Models;
using ServerSide.Models;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Formats.Asn1;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Transactions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SearchTool_ServerSide.Repository
{
    public record ClassRoute(string Route, string Kind);

    public class DrugRepository : GenericRepository<Drug>
    {
        private readonly SearchToolDBContext _context;
        private readonly IMapper _mapper;
        private readonly IMemoryCache _cache;
        private const int DemoDrugLimit = 1000;
        private const int DemoPageSizeLimit = 10;
        private const int DemoPageNumberLimit = 1;

        public DrugRepository(SearchToolDBContext context, IMapper mapper, IMemoryCache cache) : base(context)
        {
            _context = context;
            _mapper = mapper;
            _cache = cache;
        }
        public async Task<ICollection<Drug>> GetDrugsByName(string query, int page = 1, int pageSize = 20, bool isDemo = false)
        {
            int offset = (page - 1) * pageSize;

            // Set pg_trgm similarity threshold
            await _context.Database.ExecuteSqlRawAsync("SET pg_trgm.similarity_threshold = 0.3;");

            var sql = @"
        WITH source_drugs AS (
            SELECT *
            FROM ""Drugs""
            ORDER BY ""Id""
            LIMIT CASE WHEN {3} THEN {4} ELSE 2147483647 END
        ),
        ranked AS (
            SELECT *,
                ROW_NUMBER() OVER (
                    PARTITION BY name_unaccent
                    ORDER BY 
                        (
                            similarity(name_unaccent, unaccent({0})) * 0.5 +
                            ts_rank(name_tsv, plainto_tsquery(unaccent({0}))) * 0.3 +
                            CASE WHEN name_soundex = soundex(unaccent({0})) THEN 0.1 ELSE 0 END +
                            CASE WHEN name_unaccent ILIKE '%' || unaccent({0}) || '%' THEN 0.1 ELSE 0 END
                        ) DESC
                ) AS rn,
                similarity(name_unaccent, unaccent({0})) AS sim,
                ts_rank(name_tsv, plainto_tsquery(unaccent({0}))) AS ts_rank,
                soundex(name_unaccent) AS sndx
            FROM source_drugs
            WHERE name_unaccent % unaccent({0})
               OR name_tsv @@ plainto_tsquery(unaccent({0}))
               OR name_soundex = soundex(unaccent({0}))
               OR name_unaccent ILIKE '%' || unaccent({0}) || '%'
        )
        SELECT *
        FROM ranked
        WHERE rn = 1
        ORDER BY sim DESC, ts_rank DESC
        LIMIT {1} OFFSET {2};
    ";

            var results = await _context.Drugs
                .FromSqlRaw(sql, query, pageSize, offset, isDemo, DemoDrugLimit)
                .AsNoTracking()
                .ToListAsync();

            return results;
        }

        public async Task<ICollection<DrugModal>> GetClassesByName(
            string name,
            string classVersion,
            int pageNumber,
            int pageSize = 20)
        {
            // Step 1: SQL query — filter & order
            var query =
                from dc in _context.DrugClasses
                join drug in _context.Drugs on dc.DrugId equals drug.Id
                join ci in _context.ClassInfos on dc.ClassId equals ci.Id
                join ct in _context.ClassTypes on ci.ClassTypeId equals ct.Id
                where EF.Functions.ILike(ci.Name, $"%{name}%")
                   && EF.Functions.ILike(ct.Name, classVersion)
                orderby ci.Id, drug.Id
                select new
                {
                    Drug = drug,
                    ClassInfo = ci,
                    ClassType = ct
                };

            var rawResults = await query.ToListAsync();

            // Step 2: Distinct by ClassInfo.Id — in memory
            var distinctResults = rawResults
                .GroupBy(x => x.ClassInfo.Id)
                .Select(g => g.First()) // take the first (best) drug per class
                .OrderBy(x => x.ClassInfo.Id) // stable order
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new DrugModal
                {
                    Id = x.Drug.Id,
                    Name = x.Drug.Name,
                    Ndc = x.Drug.NDC,
                    Form = x.Drug.Form,
                    Strength = x.Drug.Strength,
                    ClassId = x.ClassInfo.Id,
                    ClassType = x.ClassType.Name,
                    ClassName = x.ClassInfo.Name,
                    Acq = x.Drug.ACQ,
                    Awp = x.Drug.AWP,
                    Rxcui = x.Drug.Rxcui ?? 0,
                    Route = x.Drug.Route,
                    TeCode = x.Drug.TECode,
                    Ingrdient = x.Drug.Ingrdient,
                    ApplicationNumber = x.Drug.ApplicationNumber,
                    ApplicationType = x.Drug.ApplicationType,
                    StrengthUnit = x.Drug.StrengthUnit,
                    Type = x.Drug.Type
                })
                .ToList();

            return distinctResults;
        }

        public async Task<ICollection<string>> GetAllNDCByDrugName(string name)
        {
            var items = await _context.Drugs
                  .Where(d => d.Name == name)
                .GroupBy(d => d.NDC)
                .Select(d => d.Key)
                .Distinct()
                .ToListAsync();
            return items;
        }

        // public async Task<ICollection<Insurance>> GetDrugInsurances(string name)
        // {
        //     // get insurance name by drug name
        //     var items = await _context.DrugInsurances
        //         .Where(d => d.DrugName == name)
        //         .GroupBy(d => d.InsuranceId)
        //         .Select(d => d.Key)
        //         .Distinct()
        //         .ToListAsync();

        //     // get insurance name by id     
        //     var ret = await _context.Insurances.Where(x => items.Contains(x.Id)).ToListAsync();
        //     return ret;
        // }

        public async Task<Insurance> GetInsurance(string name)
        {
            var item = await _context.Insurances.FirstOrDefaultAsync(x => x.Name == name);
            return item;

        }
        public async Task<ICollection<string>> GetAllInsuranceByNDC(string ndc)
        {
            var items = await _context.DrugInsurances
                .Where(d => d.NDCCode == ndc)
                .GroupBy(d => d.InsuranceId)
                .Select(d => d.Key)
                .Distinct()
                .ToListAsync();
            var ret = await _context.Insurances.Where(x => items.Contains(x.Id)).Select(i => i.Name).ToListAsync();
            return ret;
        }
        public class DrugClassCsv
        {
            [Name("Name")]
            public string Name { get; set; }
            [Name("ClassInfo")]
            public string ClassInfo { get; set; }
            [Name("NDC")]
            public string NDC { get; set; }
        }
        internal async Task<int> AddClassVersion(IFormFile uploadedFile, ClassTypeAddDto classTypeAddDto, bool isMultiple = false, CancellationToken ct = default)
        {
            int savedItems = 0;
            const int BATCH_SIZE = 5000;

            using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                if (uploadedFile == null || uploadedFile.Length == 0)
                    throw new ArgumentException("Uploaded file is empty or missing.", nameof(uploadedFile));

                var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    HeaderValidated = null,
                    MissingFieldFound = null,
                    BadDataFound = null,
                    DetectDelimiter = true,
                };

                // ========================================================
                // PHASE 0: Read CSV
                // ========================================================
                List<DrugClassCsv> records;
                using (var stream = uploadedFile.OpenReadStream())
                using (var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 64 * 1024, leaveOpen: false))
                using (var csv = new CsvReader(reader, csvConfig))
                {
                    records = csv.GetRecords<DrugClassCsv>().ToList();
                }

                // ========================================================
                // PHASE 1: Load all reference data upfront with AsNoTracking
                // ========================================================
                var classTypes = await _context.ClassTypes.AsNoTracking().ToDictionaryAsync(di => di.Name, di => di, ct);
                var classInfos = await _context.ClassInfos.AsNoTracking().ToDictionaryAsync(di => (di.Name, di.ClassTypeId), di => di, ct);
                var drugClasses = await _context.DrugClasses.AsNoTracking().ToDictionaryAsync(dc => (dc.ClassId, dc.DrugId), dc => dc, ct);

                var existingDrugsByNdc = (await _context.Drugs.AsNoTracking().ToListAsync(ct))
                    .GroupBy(d => d.NDC)
                    .ToDictionary(g => g.Key, g => g.First());

                // ========================================================
                // PHASE 2: Ensure ClassType exists
                // ========================================================
                ClassType tempClassType;
                if (!classTypes.TryGetValue(classTypeAddDto.Name, out tempClassType))
                {
                    tempClassType = new ClassType
                    {
                        Name = classTypeAddDto.Name,
                        Description = classTypeAddDto.Description
                    };
                    await _context.ClassTypes.AddAsync(tempClassType, ct);
                    await _context.SaveChangesAsync(ct);
                    _context.ChangeTracker.Clear();
                    classTypes[tempClassType.Name] = tempClassType;
                }

                // ========================================================
                // PHASE 3: Process records and build collections
                // ========================================================
                var newClassInfos = new List<ClassInfo>();
                var classInfosToAdd = new HashSet<string>();

                // First pass: Collect all unique ClassInfo names
                if (isMultiple)
                {
                    foreach (var record in records)
                    {
                        var allClassNames = record.ClassInfo.Trim().Split(',');
                        foreach (var item in allClassNames)
                        {
                            var className = item.Trim();
                            if (string.IsNullOrWhiteSpace(className))
                                continue;

                            var classInfoKey = (className, tempClassType.Id);
                            if (!classInfos.ContainsKey(classInfoKey) && classInfosToAdd.Add(className))
                            {
                                var classInfo = new ClassInfo { Name = className, ClassTypeId = tempClassType.Id };
                                newClassInfos.Add(classInfo);
                                classInfos[classInfoKey] = classInfo;
                            }
                        }
                    }
                }
                else
                {
                    foreach (var record in records)
                    {
                        var className = record.ClassInfo?.Trim();
                        if (string.IsNullOrWhiteSpace(className))
                            continue;

                        var classInfoKey = (className, tempClassType.Id);
                        if (!classInfos.ContainsKey(classInfoKey) && classInfosToAdd.Add(className))
                        {
                            var classInfo = new ClassInfo { Name = className, ClassTypeId = tempClassType.Id };
                            newClassInfos.Add(classInfo);
                            classInfos[classInfoKey] = classInfo;
                        }
                    }
                }

                // Bulk insert ClassInfos in batches
                if (newClassInfos.Any())
                {
                    for (int i = 0; i < newClassInfos.Count; i += BATCH_SIZE)
                    {
                        var batch = newClassInfos.Skip(i).Take(BATCH_SIZE).ToList();
                        await _context.ClassInfos.AddRangeAsync(batch, ct);
                        await _context.SaveChangesAsync(ct);
                        _context.ChangeTracker.Clear();
                    }
                    Console.WriteLine($"Added {newClassInfos.Count} new ClassInfos at {DateTime.Now}");

                    // Reload to get IDs
                    var classNames = newClassInfos.Select(ci => ci.Name).ToList();
                    var freshClassInfos = await _context.ClassInfos.AsNoTracking()
                        .Where(ci => classNames.Contains(ci.Name) && ci.ClassTypeId == tempClassType.Id)
                        .ToListAsync(ct);

                    foreach (var ci in freshClassInfos)
                    {
                        classInfos[(ci.Name, ci.ClassTypeId)] = ci;
                    }
                }

                // Second pass: Create DrugClass links
                var newDrugClasses = new List<DrugClass>();
                var drugClassKeys = new HashSet<(int classId, int drugId)>();

                if (isMultiple)
                {
                    foreach (var record in records)
                    {
                        if (!existingDrugsByNdc.TryGetValue(record.NDC, out var drug))
                            continue;

                        var allClassNames = record.ClassInfo.Trim().Split(',');
                        foreach (var item in allClassNames)
                        {
                            var className = item.Trim();
                            if (string.IsNullOrWhiteSpace(className))
                                continue;

                            var classInfoKey = (className, tempClassType.Id);
                            if (classInfos.TryGetValue(classInfoKey, out var classInfo))
                            {
                                var dcKey = (classInfo.Id, drug.Id);
                                if (!drugClasses.ContainsKey(dcKey) && drugClassKeys.Add(dcKey))
                                {
                                    var drugClass = new DrugClass
                                    {
                                        ClassId = classInfo.Id,
                                        DrugId = drug.Id
                                    };
                                    newDrugClasses.Add(drugClass);
                                    savedItems++;
                                    drugClasses[dcKey] = drugClass;
                                }
                            }
                        }
                    }
                }
                else
                {
                    foreach (var record in records)
                    {
                        record.NDC = NormalizeNdcTo11Digits(record.NDC);

                        if (!existingDrugsByNdc.TryGetValue(record.NDC, out var drug))
                            continue;

                        var className = record.ClassInfo?.Trim();
                        if (string.IsNullOrWhiteSpace(className))
                            continue;

                        var classInfoKey = (className, tempClassType.Id);
                        if (classInfos.TryGetValue(classInfoKey, out var classInfo))
                        {
                            var dcKey = (classInfo.Id, drug.Id);
                            if (!drugClasses.ContainsKey(dcKey) && drugClassKeys.Add(dcKey))
                            {
                                var drugClass = new DrugClass
                                {
                                    ClassId = classInfo.Id,
                                    DrugId = drug.Id
                                };
                                newDrugClasses.Add(drugClass);
                                savedItems++;
                                drugClasses[dcKey] = drugClass;
                            }
                        }
                    }
                }

                // Bulk insert DrugClasses in batches
                if (newDrugClasses.Any())
                {
                    for (int i = 0; i < newDrugClasses.Count; i += BATCH_SIZE)
                    {
                        var batch = newDrugClasses.Skip(i).Take(BATCH_SIZE).ToList();
                        await _context.DrugClasses.AddRangeAsync(batch, ct);
                        await _context.SaveChangesAsync(ct);
                        _context.ChangeTracker.Clear();
                    }
                    Console.WriteLine($"Added {newDrugClasses.Count} new DrugClasses at {DateTime.Now}");
                }

                // ========================================================
                // PHASE 4: Build ClassInsurance records
                // ========================================================

                // Load existing ClassInsurances for this ClassType
                var existingClassInsurances = await _context.ClassInsurances
                    .AsNoTracking()
                    .Where(x => x.ClassInfo.ClassTypeId == tempClassType.Id)
                    .ToListAsync(ct);

                var ciDict = existingClassInsurances
                    .ToDictionary(ci => (ci.InsuranceId, ci.ClassInfoId, ci.Date.Year, ci.Date.Month, ci.BranchId));

                // Load DrugClass map for the current ClassType
                var drugIds = existingDrugsByNdc.Values.Select(d => d.Id).ToHashSet();
                var allDrugClasses = await _context.DrugClasses
                    .AsNoTracking()
                    .Where(dc => drugIds.Contains(dc.DrugId) && dc.ClassInfo.ClassTypeId == tempClassType.Id)
                    .ToListAsync(ct);

                var drugClassMap = allDrugClasses
                    .GroupBy(dc => dc.DrugId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Load InsuranceRxes
                var insuranceRxs = await _context.InsuranceRxes
                    .AsNoTracking()
                    .ToDictionaryAsync(x => x.Id, ct);

                // Process ScriptItems in batches to avoid loading all at once
                var newClassInsurances = new List<ClassInsurance>();
                var ciKeys = new HashSet<(int, int, int, int, int)>();

                // Get total count for batching
                var totalScriptItems = await _context.ScriptItems.CountAsync(ct);
                var scriptBatchSize = 10000;

                for (int skip = 0; skip < totalScriptItems; skip += scriptBatchSize)
                {
                    var scriptItemsBatch = await _context.ScriptItems
                        .AsNoTracking()
                        .Include(x => x.Script)
                        .OrderBy(x => x.Id)
                        .Skip(skip)
                        .Take(scriptBatchSize)
                        .ToListAsync(ct);

                    foreach (var item in scriptItemsBatch)
                    {
                        // Skip if this drug doesn't have classes in the current ClassType
                        if (!drugClassMap.TryGetValue(item.DrugId, out var classLinks))
                            continue;

                        // Normalize date to UTC
                        DateTime recordDate = item.Script.Date.Kind == DateTimeKind.Utc
                            ? item.Script.Date
                            : DateTime.SpecifyKind(item.Script.Date, DateTimeKind.Utc);

                        decimal qty = item.Quantity > 0 ? item.Quantity : 1m;
                        DateTime yearMonth = new DateTime(recordDate.Year, recordDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);

                        // Get insurance name
                        if (!insuranceRxs.TryGetValue(item.InsuranceId, out var insuranceRx))
                            continue;

                        foreach (var classLink in classLinks)
                        {
                            var ciKey = (item.InsuranceId, classLink.ClassId, recordDate.Year, recordDate.Month, item.Script.BranchId);

                            // Skip if already exists or already added
                            if (ciDict.ContainsKey(ciKey) || !ciKeys.Add(ciKey))
                                continue;

                            var newCI = new ClassInsurance
                            {
                                InsuranceId = item.InsuranceId,
                                InsuranceName = insuranceRx.RxGroup,
                                ClassInfoId = classLink.ClassId,
                                DrugId = item.DrugId,
                                BranchId = item.Script.BranchId,
                                Date = yearMonth,
                                ScriptDateTime = recordDate,
                                ScriptCode = item.Script.ScriptCode,
                                BestNet = item.NetProfit / qty,
                                BestACQ = item.AcquisitionCost / qty,
                                BestInsurancePayment = item.InsurancePayment / qty,
                                BestPatientPayment = item.PatientPayment / qty,
                                Qty = qty
                            };

                            newClassInsurances.Add(newCI);
                            ciDict[ciKey] = newCI;
                        }
                    }

                    // Save in sub-batches if collection gets too large
                    if (newClassInsurances.Count >= BATCH_SIZE)
                    {
                        for (int i = 0; i < newClassInsurances.Count; i += BATCH_SIZE)
                        {
                            var batch = newClassInsurances.Skip(i).Take(BATCH_SIZE).ToList();
                            await _context.ClassInsurances.AddRangeAsync(batch, ct);
                            await _context.SaveChangesAsync(ct);
                            _context.ChangeTracker.Clear();
                        }
                        Console.WriteLine($"Saved {newClassInsurances.Count} ClassInsurances in progress at {DateTime.Now}");
                        newClassInsurances.Clear();
                    }
                }

                // Bulk insert remaining ClassInsurances in batches
                if (newClassInsurances.Any())
                {
                    for (int i = 0; i < newClassInsurances.Count; i += BATCH_SIZE)
                    {
                        var batch = newClassInsurances.Skip(i).Take(BATCH_SIZE).ToList();
                        await _context.ClassInsurances.AddRangeAsync(batch, ct);
                        await _context.SaveChangesAsync(ct);
                        _context.ChangeTracker.Clear();
                    }
                    Console.WriteLine($"Added {newClassInsurances.Count} new ClassInsurances at {DateTime.Now}");
                }

                transaction.Complete();
            }

            return savedItems;
        }
        public async Task SaveData(string filePath = "drug_enriched_with_group.csv")
        {
            var drugs = LoadDrugsFromCsv(filePath);

            static List<DrugCs> LoadDrugsFromCsv(string filePath)
            {

                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {

                    HasHeaderRecord = true,
                    HeaderValidated = null,
                    MissingFieldFound = null,
                };

                using var reader = new StreamReader(filePath);
                using var csv = new CsvReader(reader, config);
                return new List<DrugCs>(csv.GetRecords<DrugCs>());
            }

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var connectionString = configuration.GetConnectionString("SearchTool");

            var options = new DbContextOptionsBuilder<SearchToolDBContext>()
                .UseNpgsql(connectionString) // Change if using SQL Server
                .Options;

            using var context = new SearchToolDBContext(options);

            // **Step 1: Load Data from CSV**
            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                HeaderValidated = null,
                MissingFieldFound = null
            });

            var records = csv.GetRecords<DrugCs>().ToList();

            // **Step 2: Load Existing Drug Classes**
            var classTypes = await context.ClassTypes.ToDictionaryAsync(di => di.Name, di => di);
            var classInfos = await context.ClassInfos.ToDictionaryAsync(di => (di.Name, di.ClassTypeId), di => di);
            var drugClasses = await context.DrugClasses.ToDictionaryAsync(dc => (dc.ClassId, dc.DrugId), dc => dc);

            // **Step 3: Load Existing Drugs by NDC & Name**
            var existingDrugsByNdc = await context.Drugs
                .GroupBy(d => d.NDC)
                .ToDictionaryAsync(g => g.Key, g => g.First());

            var existingDrugsByName = await context.Drugs
                .GroupBy(d => d.Name)
                .ToDictionaryAsync(g => g.Key, g => g.First());
            var newClassTypes = new List<ClassType>();
            var newDrugs = new List<Drug>();
            var newDrugClasses = new List<DrugClass>();
            var newClassInfos = new List<ClassInfo>();
            //Name  : Description
            var addedclassTypes = new List<(string, string)>
                {
                    ("ClassV1","Exact Match"),
                    ("ClassV2","est 25% different in strength"),
                    ("ClassV3","Group_By_Class_Standardized"),
                    ("ClassV4","Cleaned with epc Names"),
                    ("ClassV5","EPC + MOA + Route"),
                    ("ClassV6","EPC + MOA like if a =>{x,y} ,b=>{y},c=>{x} then a can get y and c and b can get only and z can get a only it's only depend on the source drug search"),
                    ("ClassV7","EPC + MOA + ROUTE"),
                    ("ClassV8","EPC + MOA + ROUTE or drug Class exact match"),


                };
            foreach (var classType in addedclassTypes)
            {
                if (!classTypes.TryGetValue(classType.Item1, out var tempClassType))
                {
                    tempClassType = new ClassType
                    {
                        Name = classType.Item1,
                        Description = classType.Item2
                    };
                    newClassTypes.Add(tempClassType);
                }
            }
            if (newClassTypes.Any())
            {
                await context.ClassTypes.AddRangeAsync(newClassTypes);
                await context.SaveChangesAsync();

                newClassTypes.Clear();
            }
            classTypes = await context.ClassTypes.ToDictionaryAsync(di => di.Name, di => di);
            foreach (var record in records)
            {
                var tempClassType = new List<(string, string)>
                {
                    (record.DrugClass,"ClassV1"),
                    (record.ClassV2,"ClassV2"),
                    (record.ClassV3,"ClassV3"),
                    (record.ClassV4,"ClassV4"),
                    (record.ClassV5,"ClassV5"),
                    (record.PHARM_CLASSES,"ClassV6"),
                    (record.PHARM_CLASSES,"ClassV7"),
                    (record.PHARM_CLASSES,"ClassV8"),
                };

                for (int i = 0; i < tempClassType.Count; i++)
                {
                    var type = tempClassType[i].Item2;
                    if (classTypes.TryGetValue(type, out var classType))
                    {
                        if (type == "ClassV6")
                        {
                            var EPCMOAList = tempClassType[i].Item1.Trim().Split(",");
                            foreach (var item in EPCMOAList)
                            {
                                var className6 = item.Trim();
                                if (string.IsNullOrWhiteSpace(className6))
                                {
                                    continue;
                                }
                                var classInfoKey6 = (className6, classType.Id);
                                if (!classInfos.TryGetValue(classInfoKey6, out var classInfo6))
                                {
                                    classInfo6 = new ClassInfo { Name = className6, ClassTypeId = classType.Id };
                                    newClassInfos.Add(classInfo6);
                                    classInfos[classInfoKey6] = classInfo6;
                                }
                            }
                        }
                        else if (type == "ClassV7")
                        {
                            var raw = tempClassType[i].Item1 ?? string.Empty;
                            var items = Regex.Split(raw.Trim(), @"(?<=\])\s*,\s*")
                                             .Select(s => s.Trim())
                                             .Where(s => !string.IsNullOrWhiteSpace(s))
                                             .Distinct(StringComparer.OrdinalIgnoreCase)
                                             .ToList();
                            foreach (var item in items)
                            {
                                var className7 = item.Trim();
                                if (string.IsNullOrWhiteSpace(className7)) continue;

                                var classInfoKey7 = (className7, classType.Id);
                                if (!classInfos.TryGetValue(classInfoKey7, out var classInfo7))
                                {
                                    classInfo7 = new ClassInfo { Name = className7, ClassTypeId = classType.Id };
                                    newClassInfos.Add(classInfo7);
                                    classInfos[classInfoKey7] = classInfo7;
                                }
                            }
                            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "MoA", "EPC" };
                            var parsed = items.Select(s =>
                                             {
                                                 var m = Regex.Match(s, @"\[(?<t>[^\]]+)\]\s*$"); // last [...] tag
                                                 var t = m.Success ? m.Groups["t"].Value.Trim() : string.Empty;
                                                 return new { Text = s, Type = t };
                                             })
                                             .Where(x => allowed.Contains(x.Type))
                                             .ToList();
                            var EPCMOAClassList = new List<(string Left, string Right)>();
                            for (int a = 0; a < parsed.Count; a++)
                            {
                                for (int b = a + 1; b < parsed.Count; b++)
                                {
                                    var ta = parsed[a].Type;
                                    var tb = parsed[b].Type;
                                    if (ta.Equals(tb, StringComparison.OrdinalIgnoreCase)) continue;

                                    // Canonicalize: MoA on left, EPC on right
                                    var left = ta.Equals("MoA", StringComparison.OrdinalIgnoreCase) ? parsed[a].Text : parsed[b].Text;
                                    var right = ta.Equals("MoA", StringComparison.OrdinalIgnoreCase) ? parsed[b].Text : parsed[a].Text;

                                    EPCMOAClassList.Add((left, right));
                                }
                            }
                            foreach (var item in EPCMOAClassList)
                            {
                                var className7 = $"{item.Left.Trim()}|{item.Right.Trim()}";
                                if (string.IsNullOrWhiteSpace(className7))
                                    continue;

                                var classInfoKey7 = (className7, classType.Id);
                                if (!classInfos.TryGetValue(classInfoKey7, out var classInfo7))
                                {
                                    classInfo7 = new ClassInfo { Name = className7, ClassTypeId = classType.Id };
                                    newClassInfos.Add(classInfo7);
                                    classInfos[classInfoKey7] = classInfo7;
                                }
                            }

                        }
                        // else if (type == "ClassV10")
                        // {
                        //     var raw = tempClassType[i].Item1 ?? record.DrugClass;
                        //     var items = Regex.Split(raw.Trim(), @"(?<=\])\s*,\s*")
                        //                      .Select(s => s.Trim())
                        //                      .Where(s => !string.IsNullOrWhiteSpace(s))
                        //                      .Distinct(StringComparer.OrdinalIgnoreCase)
                        //                      .ToList();

                        //     var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "MoA", "EPC" };
                        //     var parsed = items.Select(s =>
                        //                      {
                        //                          var m = Regex.Match(s, @"\[(?<t>[^\]]+)\]\s*$"); // last [...] tag
                        //                          var t = m.Success ? m.Groups["t"].Value.Trim() : string.Empty;
                        //                          return new { Text = s, Type = t };
                        //                      })
                        //                      .Where(x => allowed.Contains(x.Type))
                        //                      .ToList();
                        //     var EPCMOAClassList = new List<(string Left, string Right)>();
                        //     for (int a = 0; a < parsed.Count; a++)
                        //     {
                        //         for (int b = a + 1; b < parsed.Count; b++)
                        //         {
                        //             var ta = parsed[a].Type;
                        //             var tb = parsed[b].Type;
                        //             if (ta.Equals(tb, StringComparison.OrdinalIgnoreCase)) continue;

                        //             // Canonicalize: MoA on left, EPC on right
                        //             var left = ta.Equals("MoA", StringComparison.OrdinalIgnoreCase) ? parsed[a].Text : parsed[b].Text;
                        //             var right = ta.Equals("MoA", StringComparison.OrdinalIgnoreCase) ? parsed[b].Text : parsed[a].Text;

                        //             EPCMOAClassList.Add((left, right));
                        //         }
                        //     }
                        //     foreach (var item in EPCMOAClassList)
                        //     {
                        //         var className7 = $"{item.Left.Trim()}|{item.Right.Trim()}|{record.Route}";

                        //         if (string.IsNullOrWhiteSpace(className7))
                        //             continue;

                        //         var classInfoKey7 = (className7, classType.Id);
                        //         if (!classInfos.TryGetValue(classInfoKey7, out var classInfo7))
                        //         {
                        //             Console.WriteLine("ClassName : " + className7);
                        //             Console.ReadKey();
                        //             classInfo7 = new ClassInfo { Name = className7, ClassTypeId = classType.Id };
                        //             newClassInfos.Add(classInfo7);
                        //             classInfos[classInfoKey7] = classInfo7;
                        //         }
                        //     }

                        // }
                        else if (type == "ClassV8")
                        {
                            var raw = tempClassType[i].Item1 ?? record.DrugClass;
                            var items = Regex.Split(raw.Trim(), @"(?<=\])\s*,\s*")
                                             .Select(s => s.Trim())
                                             .Where(s => !string.IsNullOrWhiteSpace(s))
                                             .Distinct(StringComparer.OrdinalIgnoreCase)
                                             .ToList();

                            var parsed = items.Select(s =>
                                         {
                                             var m = Regex.Match(s, @"\[(?<t>[^\]]+)\]\s*$");
                                             var t = m.Success ? m.Groups["t"].Value.Trim() : string.Empty;
                                             return new { Text = s, Type = t };
                                         })
                                         .ToList();

                            var moas = parsed.Where(x => x.Type.Equals("MoA", StringComparison.OrdinalIgnoreCase))
                                             .Select(x => x.Text)
                                             .Distinct(StringComparer.OrdinalIgnoreCase)
                                             .ToList();

                            var epcs = parsed.Where(x => x.Type.Equals("EPC", StringComparison.OrdinalIgnoreCase))
                                             .Select(x => x.Text)
                                             .Distinct(StringComparer.OrdinalIgnoreCase)
                                             .ToList();

                            // Route comes from the record directly (no split)
                            var route = record?.Route?.Trim(); // <-- route is here
                            var routeExists = !string.IsNullOrWhiteSpace(route);

                            // Collect all generated “route strings” and route records
                            var generated = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            var routes = new List<ClassRoute>();

                            void AddCombo(string kind, params string[] parts)
                            {
                                var tokens = parts.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
                                if (tokens.Length == 0) return;

                                // Canonical string (joined with '|'), e.g., "MoA|EPC|Route"
                                var routeString = string.Join("|", tokens);
                                if (!generated.Add(routeString)) return;

                                // Ensure ClassInfo for the combo string too
                                var k = (routeString, classType.Id);
                                if (!classInfos.TryGetValue(k, out var ci))
                                {
                                    ci = new ClassInfo { Name = routeString, ClassTypeId = classType.Id };
                                    newClassInfos.Add(ci);
                                    classInfos[k] = ci;
                                }

                                // Record the route (single string, no splitting)
                                routes.Add(new ClassRoute(routeString, kind));
                            }

                            // === Build combinations per your rules ===

                            // EPC + MoA (+ Route if present)
                            if (moas.Count > 0 && epcs.Count > 0)
                            {
                                foreach (var moa in moas)
                                {
                                    foreach (var epc in epcs)
                                    {
                                        if (routeExists) AddCombo("MoA_EPC_ROUTE", moa, epc, route);  // MoA|EPC|Route
                                        AddCombo("MoA_EPC", moa, epc);                                // MoA|EPC
                                    }
                                }
                            }

                            // EPC + Route
                            if (epcs.Count > 0 && routeExists)
                            {
                                foreach (var epc in epcs)
                                    AddCombo("EPC_ROUTE", epc, route);
                            }
                            if (moas.Count > 0 && routeExists)
                            {
                                foreach (var moa in moas)
                                    AddCombo("MOA_ROUTE", moa, route);
                            }

                            if (epcs.Count > 0)
                            {
                                foreach (var epc in epcs)
                                    AddCombo("EPC", epc);
                            }
                            if (moas.Count > 0)
                            {
                                foreach (var moa in moas)
                                    AddCombo("MOA", moa);
                            }
                            if (moas.Count == 0 && epcs.Count == 0)
                            {
                                if (routeExists)
                                {
                                    AddCombo("ROUTE", route);
                                }
                                else
                                {
                                    // Totally empty → fallback to record.DrugClass
                                    var fallback = record?.DrugClass?.Trim();
                                    if (!string.IsNullOrWhiteSpace(fallback))
                                        AddCombo("FALLBACK_DRUGCLASS", fallback);
                                }
                            }

                        }
                        else
                        {
                            var className = tempClassType[i].Item1;
                            if (string.IsNullOrWhiteSpace(className))
                            {
                                // Skip if className is null or empty
                                continue;
                            }
                            var classInfoKey = (className, classType.Id);
                            if (!classInfos.TryGetValue(classInfoKey, out var classInfo))
                            {
                                classInfo = new ClassInfo { Name = className, ClassTypeId = classType.Id };
                                newClassInfos.Add(classInfo);
                                classInfos[classInfoKey] = classInfo;
                            }
                        }
                    }
                }



            }

            // Batch insert new drug classes
            if (newClassInfos.Any())
            {
                await context.ClassInfos.AddRangeAsync(newClassInfos);
                await context.SaveChangesAsync();
                Console.WriteLine($"Added {newClassInfos.Count} new drug classes at {DateTime.Now}");
            }
            const int batchSize = 50000;
            foreach (var record in records)
            {
                record.Name = record.Name.ToUpper();
                string tempNdc = NormalizeNdcTo11Digits(record.NDC);

                // **Check if Drug Exists by NDC**
                if (existingDrugsByNdc.ContainsKey(tempNdc))
                {
                    continue; // Skip existing drug
                }

                {
                    // **Create Drug Class if Missing**
                    var newDrug = new Drug
                    {
                        Name = record.Name,
                        NDC = tempNdc,
                        Form = record.Form,
                        Strength = record.Strength,
                        ACQ = record.ACQ ?? 0,
                        AWP = record.AWP ?? 0,
                        Rxcui = record.Rxcui,
                        Route = record.Route,
                        Ingrdient = record.Ingrdient,
                        TECode = record.TECode,
                        ApplicationNumber = record.ApplicationNumber,
                        ApplicationType = record.ApplicationType,
                        StrengthUnit = record.Unit,
                        Type = record.Type
                    };

                    newDrugs.Add(newDrug);
                    existingDrugsByNdc[tempNdc] = newDrug; // Add to dictionary
                }

                // **Batch Processing**
                if (newDrugs.Count >= batchSize)
                {
                    await context.Drugs.AddRangeAsync(newDrugs);
                    await context.SaveChangesAsync();
                    Console.WriteLine($"Processed batch of {batchSize} drugs at {DateTime.Now}");
                    newDrugs.Clear();
                }
            }

            if (newDrugs.Any())
            {
                await context.Drugs.AddRangeAsync(newDrugs);
                await context.SaveChangesAsync();
                Console.WriteLine($"Processed batch of {newDrugs.Count} drugs at {DateTime.Now}");

                // Refresh the dictionary to include new drugs with their generated IDs
                existingDrugsByNdc = await context.Drugs
                    .GroupBy(d => d.NDC)
                    .ToDictionaryAsync(g => g.Key, g => g.First());
                newDrugs.Clear();
            }
            foreach (var record in records)
            {
                var tempClassType = new List<(string, string)>
                {
                    (record.DrugClass,"ClassV1"),
                    (record.ClassV2,"ClassV2"),
                    (record.ClassV3,"ClassV3"),
                    (record.ClassV4,"ClassV4"),
                    (record.ClassV5,"ClassV5"),
                    (record.PHARM_CLASSES,"ClassV6"),
                    (record.PHARM_CLASSES,"ClassV7"),
                    (record.PHARM_CLASSES,"ClassV8"),

                };

                string tempNdc = NormalizeNdcTo11Digits(record.NDC);
                if (existingDrugsByNdc.TryGetValue(tempNdc, out var drug))
                {

                    for (int i = 0; i < tempClassType.Count(); i++)
                    {
                        var type = tempClassType[i].Item2;
                        if (classTypes.TryGetValue(type, out var classType))
                        {
                            if (classType.Name == "ClassV6")
                            {
                                var EPCMOAList = tempClassType[i].Item1.Trim().Split(",");
                                foreach (var epcmoa in EPCMOAList)
                                {
                                    var classInfoKey6 = (epcmoa.Trim(), classType.Id);
                                    if (classInfos.ContainsKey(classInfoKey6))
                                    {
                                        var classInfo6 = classInfos[classInfoKey6];
                                        if (!drugClasses.ContainsKey((classInfo6.Id, drug.Id)))
                                        {
                                            var newDrugClass = new DrugClass
                                            {
                                                ClassId = classInfo6.Id,
                                                DrugId = drug.Id
                                            };
                                            drugClasses[(classInfo6.Id, drug.Id)] = newDrugClass;
                                            newDrugClasses.Add(newDrugClass);
                                        }
                                    }
                                }
                            }
                            else if (classType.Name == "ClassV7")
                            {
                                var raw = tempClassType[i].Item1 ?? string.Empty;

                                // Split only on commas that follow a closing bracket: "...] , ..."
                                var items = Regex.Split(raw.Trim(), @"(?<=\])\s*,\s*")
                                                 .Select(s => s.Trim())
                                                 .Where(s => !string.IsNullOrWhiteSpace(s))
                                                 .Distinct(StringComparer.OrdinalIgnoreCase)
                                                 .ToList();

                                // Ensure ClassInfo exists for each unique item (your original logic)
                                foreach (var item in items)
                                {
                                    var className7 = item.Trim();
                                    if (string.IsNullOrWhiteSpace(className7)) continue;

                                    var classInfoKey7 = (className7, classType.Id);
                                    if (!classInfos.TryGetValue(classInfoKey7, out var classInfo7))
                                    {
                                        classInfo7 = new ClassInfo { Name = className7, ClassTypeId = classType.Id };
                                        newClassInfos.Add(classInfo7);
                                        classInfos[classInfoKey7] = classInfo7;
                                    }
                                }

                                // Keep only MoA/EPC entries and parse their type tag
                                var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "MoA", "EPC" };
                                var parsed = items.Select(s =>
                                                 {
                                                     var m = Regex.Match(s, @"\[(?<t>[^\]]+)\]\s*$"); // last [...] tag
                                                     var t = m.Success ? m.Groups["t"].Value.Trim() : string.Empty;
                                                     return new { Text = s, Type = t };
                                                 })
                                                 .Where(x => allowed.Contains(x.Type))
                                                 .ToList();

                                var EPCMOAClassList = new List<(string Left, string Right)>();
                                for (int a = 0; a < parsed.Count; a++)
                                {
                                    for (int b = a + 1; b < parsed.Count; b++)
                                    {
                                        var ta = parsed[a].Type;
                                        var tb = parsed[b].Type;
                                        if (ta.Equals(tb, StringComparison.OrdinalIgnoreCase)) continue;

                                        // Canonicalize: MoA on left, EPC on right
                                        var left = ta.Equals("MoA", StringComparison.OrdinalIgnoreCase) ? parsed[a].Text : parsed[b].Text;
                                        var right = ta.Equals("MoA", StringComparison.OrdinalIgnoreCase) ? parsed[b].Text : parsed[a].Text;

                                        EPCMOAClassList.Add((left, right));
                                    }
                                }
                                foreach (var epcmoa in EPCMOAClassList)
                                {
                                    var classInfoKey6 = ($"{epcmoa.Left.Trim()}|{epcmoa.Right.Trim()}", classType.Id);
                                    if (classInfos.ContainsKey(classInfoKey6))
                                    {
                                        var classInfo6 = classInfos[classInfoKey6];
                                        if (!drugClasses.ContainsKey((classInfo6.Id, drug.Id)))
                                        {
                                            var newDrugClass = new DrugClass
                                            {
                                                ClassId = classInfo6.Id,
                                                DrugId = drug.Id
                                            };
                                            drugClasses[(classInfo6.Id, drug.Id)] = newDrugClass;
                                            newDrugClasses.Add(newDrugClass);
                                        }
                                    }
                                }
                            }

                            // if (classType.Name == "ClassV10")
                            // {
                            //     var raw = tempClassType[i].Item1 ?? record.DrugClass;

                            //     // Split only on commas that follow a closing bracket: "...] , ..."
                            //     var items = Regex.Split(raw.Trim(), @"(?<=\])\s*,\s*")
                            //                      .Select(s => s.Trim())
                            //                      .Where(s => !string.IsNullOrWhiteSpace(s))
                            //                      .Distinct(StringComparer.OrdinalIgnoreCase)
                            //                      .ToList();

                            //     // Ensure ClassInfo exists for each unique item (your original logic)
                            //     foreach (var item in items)
                            //     {
                            //         var className7 = item.Trim();
                            //         if (string.IsNullOrWhiteSpace(className7)) continue;

                            //         var classInfoKey7 = (className7, classType.Id);
                            //         if (!classInfos.TryGetValue(classInfoKey7, out var classInfo7))
                            //         {
                            //             classInfo7 = new ClassInfo { Name = className7, ClassTypeId = classType.Id };
                            //             newClassInfos.Add(classInfo7);
                            //             classInfos[classInfoKey7] = classInfo7;
                            //         }
                            //     }

                            //     // Keep only MoA/EPC entries and parse their type tag
                            //     var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "MoA", "EPC" };
                            //     var parsed = items.Select(s =>
                            //                      {
                            //                          var m = Regex.Match(s, @"\[(?<t>[^\]]+)\]\s*$"); // last [...] tag
                            //                          var t = m.Success ? m.Groups["t"].Value.Trim() : string.Empty;
                            //                          return new { Text = s, Type = t };
                            //                      })
                            //                      .Where(x => allowed.Contains(x.Type))
                            //                      .ToList();

                            //     var EPCMOAClassList = new List<(string Left, string Right)>();
                            //     for (int a = 0; a < parsed.Count; a++)
                            //     {
                            //         for (int b = a + 1; b < parsed.Count; b++)
                            //         {
                            //             var ta = parsed[a].Type;
                            //             var tb = parsed[b].Type;
                            //             if (ta.Equals(tb, StringComparison.OrdinalIgnoreCase)) continue;

                            //             // Canonicalize: MoA on left, EPC on right
                            //             var left = ta.Equals("MoA", StringComparison.OrdinalIgnoreCase) ? parsed[a].Text : parsed[b].Text;
                            //             var right = ta.Equals("MoA", StringComparison.OrdinalIgnoreCase) ? parsed[b].Text : parsed[a].Text;

                            //             EPCMOAClassList.Add((left, right));
                            //         }
                            //     }
                            //     foreach (var epcmoa in EPCMOAClassList)
                            //     {
                            //         var classInfoKey6 = ($"{epcmoa.Left.Trim()}|{epcmoa.Right.Trim()}|{record.Route}", classType.Id);
                            //         if (classInfos.ContainsKey(classInfoKey6))
                            //         {
                            //             var classInfo6 = classInfos[classInfoKey6];
                            //             if (!drugClasses.ContainsKey((classInfo6.Id, drug.Id)))
                            //             {
                            //                 var newDrugClass = new DrugClass
                            //                 {
                            //                     ClassId = classInfo6.Id,
                            //                     DrugId = drug.Id
                            //                 };
                            //                 drugClasses[(classInfo6.Id, drug.Id)] = newDrugClass;
                            //                 newDrugClasses.Add(newDrugClass);
                            //             }
                            //         }
                            //     }
                            // }

                            else if (classType.Name == "ClassV8")
                            {
                                // Raw value
                                var raw = tempClassType[i].Item1 ?? record.DrugClass;

                                // Split only on commas that follow a closing bracket: "...] , ..."
                                var items = Regex.Split(raw.Trim(), @"(?<=\])\s*,\s*")
                                                 .Select(s => s.Trim())
                                                 .Where(s => !string.IsNullOrWhiteSpace(s))
                                                 .Distinct(StringComparer.OrdinalIgnoreCase)
                                                 .ToList();

                                // Parse trailing bracket tag (e.g., "... [MoA]" -> "MoA")
                                var parsed = items.Select(s =>
                                             {
                                                 var m = Regex.Match(s, @"\[(?<t>[^\]]+)\]\s*$");
                                                 var t = m.Success ? m.Groups["t"].Value.Trim() : string.Empty;
                                                 return new { Text = s, Type = t };
                                             })
                                             .ToList();

                                var moas = parsed.Where(x => x.Type.Equals("MoA", StringComparison.OrdinalIgnoreCase))
                                                 .Select(x => x.Text)
                                                 .Distinct(StringComparer.OrdinalIgnoreCase)
                                                 .ToList();

                                var epcs = parsed.Where(x => x.Type.Equals("EPC", StringComparison.OrdinalIgnoreCase))
                                                 .Select(x => x.Text)
                                                 .Distinct(StringComparer.OrdinalIgnoreCase)
                                                 .ToList();

                                // Route comes from the record directly (no split)
                                var route = record?.Route?.Trim(); // <-- route is here
                                var routeExists = !string.IsNullOrWhiteSpace(route);

                                // Collect all generated “route strings” and route records
                                var generated = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                                var routes = new List<ClassRoute>();

                                void AddCombo(string kind, params string[] parts)
                                {
                                    var tokens = parts.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
                                    if (tokens.Length == 0) return;

                                    // Canonical string (joined with '|'), e.g., "MoA|EPC|Route"
                                    var routeString = string.Join("|", tokens);
                                    if (!generated.Add(routeString)) return;

                                    // Ensure ClassInfo for the combo string too
                                    var k = (routeString, classType.Id);
                                    if (classInfos.TryGetValue(k, out var ci))
                                    {
                                        var classInfo6 = ci;
                                        if (!drugClasses.ContainsKey((classInfo6.Id, drug.Id)))
                                        {
                                            var newDrugClass = new DrugClass
                                            {
                                                ClassId = classInfo6.Id,
                                                DrugId = drug.Id
                                            };
                                            drugClasses[(classInfo6.Id, drug.Id)] = newDrugClass;
                                            newDrugClasses.Add(newDrugClass);
                                        }
                                    }

                                }

                                // === Build combinations per your rules ===

                                // EPC + MoA (+ Route if present)
                                if (moas.Count > 0 && epcs.Count > 0)
                                {
                                    foreach (var moa in moas)
                                    {
                                        foreach (var epc in epcs)
                                        {
                                            if (routeExists) AddCombo("MoA_EPC_ROUTE", moa, epc, route);  // MoA|EPC|Route
                                            else
                                                AddCombo("MoA_EPC", moa, epc);                                // MoA|EPC
                                        }
                                    }
                                }

                                // EPC + Route
                                else if (epcs.Count > 0 && routeExists)
                                {
                                    foreach (var epc in epcs)
                                        AddCombo("EPC_ROUTE", epc, route);                                // EPC|Route
                                }

                                // MoA + Route
                                else if (moas.Count > 0 && routeExists)
                                {
                                    foreach (var moa in moas)
                                        AddCombo("MOA_ROUTE", moa, route);                                // MoA|Route
                                }

                                // EPC (singles)
                                else if (epcs.Count > 0)
                                {
                                    foreach (var epc in epcs)
                                        AddCombo("EPC", epc);                                             // EPC
                                }

                                // MoA (singles)
                                else if (moas.Count > 0)
                                {
                                    foreach (var moa in moas)
                                        AddCombo("MOA", moa);                                             // MoA
                                }

                                // If there is NO EPC and NO MoA → take what's available
                                else if (moas.Count == 0 && epcs.Count == 0)
                                {
                                    if (routeExists)
                                    {
                                        AddCombo("ROUTE", record.DrugClass);                                         // Route only
                                    }
                                    else
                                    {
                                        // Totally empty → fallback to record.DrugClass
                                        var fallback = record?.DrugClass?.Trim();
                                        if (!string.IsNullOrWhiteSpace(fallback))
                                            AddCombo("FALLBACK_DRUGCLASS", fallback);                     // DrugClass only
                                    }
                                }
                            }
                            else
                            {
                                var className = tempClassType[i].Item1;

                                var classInfoKey = (className, classType.Id);

                                if (classInfos.TryGetValue(classInfoKey, out var classInfo))
                                {
                                    if (!drugClasses.ContainsKey((classInfo.Id, drug.Id)))
                                    {
                                        var newDrugClass = new DrugClass
                                        {
                                            ClassId = classInfo.Id,
                                            DrugId = drug.Id
                                        };
                                        drugClasses[(classInfo.Id, drug.Id)] = newDrugClass;
                                        newDrugClasses.Add(newDrugClass);
                                    }
                                }
                            }

                        }
                    }
                }
                if (newDrugClasses.Count >= batchSize)
                {
                    await context.DrugClasses.AddRangeAsync(newDrugClasses);
                    await context.SaveChangesAsync();
                    Console.WriteLine($"Processed batch of {batchSize} drugCLasses at {DateTime.Now}");
                    newDrugClasses.Clear();
                }
            }
            if (newDrugClasses.Any())
            {
                await context.DrugClasses.AddRangeAsync(newDrugClasses);
                await context.SaveChangesAsync();
                Console.WriteLine($"Processed final batch of {newDrugClasses.Count} newDrugClasses at {DateTime.Now}");
            }

        }


        public async Task AddMediCare()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            string filePath = @"Medical_with_NADAC_Data.xlsx";
            if (!File.Exists(filePath))
            {
                Console.WriteLine("File not found.");
                return;
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            Console.WriteLine("Opening the file...");

            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
            using var reader = ExcelReaderFactory.CreateReader(stream);
            using var reader1 = ExcelReaderFactory.CreateReader(stream);
            using var reader2 = ExcelReaderFactory.CreateReader(stream);

            int rowIndex = 0;
            var existingDrugsByNdc = await _context.Drugs
               .GroupBy(d => d.NDC)
               .ToDictionaryAsync(g => g.Key, g => g.First());
            var existingDrugClass = await _context.DrugClasses.ToDictionaryAsync(dc => (dc.ClassId, dc.DrugId));
            var classTypes = await _context.ClassTypes.ToDictionaryAsync(dc => dc.Name);
            var drugBranchDict = await _context.DrugBranches.ToDictionaryAsync(g => (g.BranchId, g.DrugNDC));
            var diDict = await _context.DrugInsurances.ToDictionaryAsync(di => (di.InsuranceId, di.DrugId, di.BranchId));
            var classInfoDict = await _context.ClassInfos.ToDictionaryAsync(ci => (ci.Name, ci.ClassTypeId));
            var existingDrugMediByNDC = await _context.DrugMedis
                                                .GroupBy(d => d.DrugNDC)
                                                .ToDictionaryAsync(g => g.Key, g => g.First());
            var drugMedis = new List<DrugMedi>();
            var unmatched = new List<string>();
            var drugBranches = new List<DrugBranch>();
            var newDrugInsurances = new List<DrugInsurance>();
            var newDrug = new List<Drug>();
            var newDrugClass = new List<DrugClass>();
            var newClassInfo = new List<ClassInfo>();
            var insuranceBin = await _context.Insurances.FirstOrDefaultAsync(x => x.Bin == "022659");
            var tempclassTypes = new List<string>
                {
                    "ClassV1",
                    "ClassV2",
                    "ClassV3",
                    "ClassV4",
                    "classV5"
                };
            var tempDrugNdcs = new List<string>();

            if (insuranceBin == null)
            {
                _context.Insurances.Add(new Insurance
                {
                    Bin = "022659",
                    Name = "Medi-Cal"
                });
                await _context.SaveChangesAsync();
                insuranceBin = await _context.Insurances.FirstOrDefaultAsync(x => x.Bin == "022659");
                var insurancePcn = await _context.InsurancePCNs.FirstOrDefaultAsync(x => x.InsuranceId == insuranceBin.Id);
                if (insurancePcn == null)
                {
                    _context.InsurancePCNs.Add(new InsurancePCN
                    {
                        PCN = "Medi-Cal",
                        InsuranceId = insuranceBin.Id
                    });
                    await _context.SaveChangesAsync();
                }
                insurancePcn = await _context.InsurancePCNs.FirstOrDefaultAsync(x => x.InsuranceId == insuranceBin.Id);
                var insuranceRx = await _context.InsuranceRxes.FirstOrDefaultAsync(x => x.InsurancePCNId == insurancePcn.Id);
                if (insuranceRx == null)
                {
                    _context.InsuranceRxes.Add(new InsuranceRx
                    {
                        RxGroup = "Medi-Cal",
                        InsurancePCNId = insurancePcn.Id
                    });
                    await _context.SaveChangesAsync();
                }
            }
            var insurance = await _context.InsuranceRxes.FirstOrDefaultAsync(x => x.RxGroup == "Medi-Cal");

            while (reader1.Read())
            {
                var productId = NormalizeNdcTo11Digits(reader1.GetValue(0)?.ToString());
                var labelName = reader1.GetValue(1)?.ToString();
                for (int i = 0; i < tempclassTypes.Count(); i++)
                {
                    if (classTypes.TryGetValue(tempclassTypes[i], out var classType))
                    {
                        var classInfoKey = (labelName, classType.Id);
                        if (!classInfoDict.TryGetValue(classInfoKey, out var classInfo))
                        {
                            classInfo = new ClassInfo
                            {
                                Name = labelName,
                                ClassTypeId = classType.Id
                            };

                            newClassInfo.Add(classInfo);
                            classInfoDict[classInfoKey] = classInfo;
                        }
                    }
                }


            }
            if (newClassInfo.Any())
            {
                await _context.ClassInfos.AddRangeAsync(newClassInfo);
                await _context.SaveChangesAsync();
                Console.WriteLine($"Processed batch of {newClassInfo.Count} drugs at {DateTime.Now}");
            }

            rowIndex = 0;
            // Reset the row index for the main processing loop
            while (reader2.Read())
            {
                rowIndex++;
                if (rowIndex == 1)
                    continue;
                var productId = NormalizeNdcTo11Digits(reader2.GetValue(0)?.ToString());
                var labelName = reader2.GetValue(1)?.ToString();
                var priorAuth = reader2.GetValue(3)?.ToString();
                var extended = reader2.GetValue(4)?.ToString();
                var CostringTier = reader2.GetValue(5)?.ToString();
                var NotComp = reader2.GetValue(6)?.ToString();
                var ccp = reader2.GetValue(7)?.ToString();
                var insurancePay = reader2.GetValue(8)?.ToString();
                var unit = reader2.GetValue(9)?.ToString();
                var dateTime = reader2.GetValue(10)?.ToString();
                if (!existingDrugsByNdc.TryGetValue(productId, out var x))
                {

                    var newDrugItem = new Drug
                    {
                        NDC = productId,
                        Name = labelName,

                        Form = "NA",
                        Strength = "NA",
                        ACQ = 0,
                        AWP = 0,
                        Rxcui = 0,
                        Route = "NA",
                        Ingrdient = "NA",
                        TECode = "NA",
                        ApplicationNumber = "NA",
                        ApplicationType = "NA",
                        StrengthUnit = unit,
                        Type = "NA"
                    };
                    newDrug.Add(newDrugItem);
                    existingDrugsByNdc[productId] = newDrugItem;
                    tempDrugNdcs.Add(productId);
                }

            }
            if (newDrug.Any())
            {
                await _context.Drugs.AddRangeAsync(newDrug);
                await _context.SaveChangesAsync();
                Console.WriteLine($"Processed batch of {newDrug.Count} drugs at {DateTime.Now}");
            }
            foreach (var tempNDC in tempDrugNdcs)
            {
                if (existingDrugsByNdc.TryGetValue(tempNDC, out var drug))
                {
                    for (int i = 0; i < tempclassTypes.Count(); i++)
                    {
                        if (classTypes.TryGetValue(tempclassTypes[i], out var classType))
                        {
                            var classInfoKey = (drug.Name, classType.Id);
                            if (classInfoDict.TryGetValue(classInfoKey, out var classInfo))
                            {
                                var drugClassKey = (classInfo.Id, drug.Id);
                                if (!existingDrugClass.TryGetValue(drugClassKey, out var existingClass))
                                {
                                    existingClass = new DrugClass
                                    {
                                        DrugId = drugClassKey.Item2,
                                        ClassId = drugClassKey.Item1
                                    };
                                    existingDrugClass[drugClassKey] = existingClass;
                                }
                            }
                        }
                    }
                }
            }
            rowIndex = 0;
            while (reader.Read())
            {
                rowIndex++;
                if (rowIndex == 1)
                    continue;
                var productId = NormalizeNdcTo11Digits(reader.GetValue(0)?.ToString());
                var labelName = reader.GetValue(1)?.ToString();
                var priorAuth = reader.GetValue(3)?.ToString();
                var extended = reader.GetValue(4)?.ToString();
                var CostringTier = reader.GetValue(5)?.ToString();
                var NotComp = reader.GetValue(6)?.ToString();
                var ccp = reader.GetValue(7)?.ToString();
                var insurancePay = reader.GetValue(8)?.ToString();
                var unit = reader.GetValue(9)?.ToString();
                var dateTime = reader.GetValue(10)?.ToString();

                if (existingDrugsByNdc.TryGetValue(productId, out var drug))
                {

                    var diKey = (insurance.Id, drug.Id, 1);
                    if (!diDict.TryGetValue(diKey, out var existingDI))
                    {
                        var newDI = new DrugInsurance
                        {
                            InsuranceId = insurance.Id,
                            DrugId = drug.Id,
                            BranchId = 6,
                            NDCCode = drug.NDC,
                            Net = 0,
                            Date = dateTime != null
                                ? (DateTime.TryParseExact(dateTime,
                                                        new[] { "MM-dd-yy", "M/d/yyyy h:mm:ss tt", "yyyy-MM-dd" },
                                                        CultureInfo.InvariantCulture,
                                                        DateTimeStyles.None,
                                                        out var parsedDate)
                                    ? parsedDate.ToUniversalTime()
                                    : DateTime.UtcNow)
                                : DateTime.UtcNow,
                            Prescriber = "",
                            Quantity = 0,
                            AcquisitionCost = 0,
                            Discount = 0,
                            InsurancePayment = insurancePay != null
                                ? (decimal.Parse(insurancePay) * 0.9m)
                                : 0m,
                            PatientPayment = 0,
                        };
                        newDrugInsurances.Add(newDI);
                        diDict.Add(diKey, newDI);
                    }




                    if (!drugBranchDict.ContainsKey((1, drug.NDC)))
                    {
                        var newDrugBranch = new DrugBranch
                        {
                            BranchId = 6,
                            DrugNDC = drug.NDC
                        };
                        drugBranches.Add(newDrugBranch);
                    }
                    if (!existingDrugMediByNDC.ContainsKey(productId))
                    {
                        // Console.WriteLine($"Adding new DrugMedi for NDC: {drug.Id}");
                        // Console.ReadKey();
                        var newDrugMedei = new DrugMedi
                        {
                            DrugId = drug.Id,
                            DrugNDC = drug.NDC,
                            PriorAuthorization = priorAuth,
                            ExtendedDuration = extended,
                            CostCeilingTier = CostringTier,
                            NonCapitatedDrugIndicator = NotComp,
                            CCSPanelAuthority = ccp ?? "NA"
                        };
                        drugMedis.Add(newDrugMedei);
                    }
                    // Console.WriteLine($"Product ID: {productId}, Prior Auth: {priorAuth} " +
                    //     $", Extended: {extended}, Costring Tier: {CostringTier}, Not Comp: {NotComp}");
                }
                else
                {
                    //store this in list after that at file.txt
                    unmatched.Add(productId);
                }
            }
            // Save unmatched NDCs to a file
            if (unmatched.Any())
            {
                File.WriteAllLines("unmatched.txt", unmatched);
                Console.WriteLine($"Unmatched NDCs saved to unmatched.txt");
            }


            if (drugBranches.Any())
            {
                await _context.DrugBranches.AddRangeAsync(drugBranches);
                await _context.SaveChangesAsync();
                Console.WriteLine($"Processed batch of {drugBranches.Count} drugs at {DateTime.Now}");
            }
            if (drugMedis.Any())
            {
                await _context.DrugMedis.AddRangeAsync(drugMedis);
                await _context.SaveChangesAsync();
                Console.WriteLine($"Processed batch of {drugMedis.Count} drugs at {DateTime.Now}");
            }
            if (newDrugInsurances.Any())
            {
                await _context.DrugInsurances.AddRangeAsync(newDrugInsurances);
                await _context.SaveChangesAsync();
                Console.WriteLine($"Processed batch of {newDrugInsurances.Count} drugs at {DateTime.Now}");
            }

            stopwatch.Stop();
            Console.WriteLine($"Finished in {stopwatch.ElapsedMilliseconds} ms");
        }


        public async Task<int> AddScripts(ICollection<ScriptAddDto> scriptAddDtos)
        {

            var records = scriptAddDtos;

            // Keep only records that yield a valid Drug.
            var processedRecords = new List<ScriptAddDto>();

            // ========================================================
            // PHASE 1: Process Principal Entities – Insurances & Drugs
            // ========================================================
            // Preload existing Insurances, Drugs, and DrugClasses.
            var insuranceDict = await _context.Insurances.ToDictionaryAsync(i => i.Bin);
            var insurancePCNDict = await _context.InsurancePCNs.ToDictionaryAsync(i => i.PCN);
            var insuranceRxDict = await _context.InsuranceRxes.ToDictionaryAsync(i => i.RxGroup);
            var drugsFromDb = await _context.Drugs.ToListAsync();
            var drugDict = drugsFromDb
                                    .GroupBy(d => d.NDC)
                                    .ToDictionary(g => g.Key, g => g.First());
            var classInfoDict = await _context.ClassInfos.ToDictionaryAsync(ci => ci.Id);
            var drugByNameDict = drugsFromDb
                                    .GroupBy(d => d.Name)
                                    .ToDictionary(g => g.Key, g => g.First());
            var drugClassDict = await _context.DrugClasses.ToDictionaryAsync(dc => new { dc.ClassId, dc.DrugId });
            var newInsurances = new List<Insurance>();
            var newInsurancePCNs = new List<InsurancePCN>();
            var newInsuranceRxes = new List<InsuranceRx>();

            var newDrugs = new List<Drug>();
            var newDrugClasses = new List<DrugClass>();
            int batchSize = 1000, countPhase1 = 0;
            var newScriptsDrugs = new List<(string, List<int>)>();

            foreach (var record in records)
            {
                record.Bin = record.Bin.ToUpper();
                record.PCN = record.PCN.ToUpper();
                record.RxGroup = record.RxGroup.ToUpper();
                record.DrugName = record.DrugName.ToUpper();
                if (record.Bin.Length < 6)
                {
                    record.Bin = record.Bin.PadLeft(6, '0');
                }
                if (record.PCN.Length < 1)
                {
                    record.PCN = record.Bin + "(Other)";
                }
                if (record.RxGroup.Length < 1)
                {

                    record.RxGroup = record.PCN + "(Other)";

                }
                record.RxGroup = record.RxGroup.Trim();
                // Normalize NDC.
                record.NDCCode = NormalizeNdcTo11Digits(record.NDCCode);
                if (string.IsNullOrWhiteSpace(record.NDCCode) || record.NDCCode == "00000000000")
                {
                    Console.WriteLine($"Skipping record with invalid NDC: {record.NDCCode}");
                    continue;
                }
                // ---- Process Insurance
                if (!insuranceDict.ContainsKey(record.Bin))
                {
                    var ins = new Insurance { Bin = record.Bin };
                    newInsurances.Add(ins);
                    insuranceDict[record.Bin] = ins; // will have generated Id after saving
                }
                // ---- Process Drug
                Drug drug = null;
                if (!drugDict.TryGetValue(record.NDCCode, out drug))
                {
                    if (drugByNameDict.TryGetValue(record.DrugName, out var tempDrug))
                    {
                        drug = new Drug
                        {
                            Name = record.DrugName,
                            NDC = record.NDCCode,
                            Form = tempDrug.Form,
                            Strength = tempDrug.Strength,
                            ACQ = record.AcquisitionCost,
                            AWP = 0,
                            Rxcui = tempDrug.Rxcui,
                            Route = tempDrug.Route,
                            Ingrdient = tempDrug.Ingrdient,
                            TECode = tempDrug.TECode,
                            ApplicationNumber = tempDrug.ApplicationNumber,
                            ApplicationType = tempDrug.ApplicationType
                        };
                        var classInfos = await _context.DrugClasses.Where(x => x.DrugId == tempDrug.Id).Select(x => x.ClassId).ToListAsync();
                        newDrugs.Add(drug);
                        newScriptsDrugs.Add((drug.NDC, classInfos));
                        drugDict[record.NDCCode] = drug;
                    }
                    else
                    {
                        Console.WriteLine($"Skipping record: Drug with NDC {record.NDCCode} not found.");
                        continue;
                    }
                }
                processedRecords.Add(record);
                countPhase1++;
                if (countPhase1 % batchSize == 0)
                {
                    if (newInsurances.Any())
                    {
                        _context.Insurances.AddRange(newInsurances);
                        await _context.SaveChangesAsync();
                        newInsurances.Clear();
                    }
                    if (newDrugs.Any())
                    {
                        _context.Drugs.AddRange(newDrugs);
                        await _context.SaveChangesAsync();
                        newDrugs.Clear();
                    }
                }
            }


            if (newInsurances.Any())
            {
                _context.Insurances.AddRange(newInsurances);
                await _context.SaveChangesAsync();
            }
            if (newDrugs.Any())
            {
                _context.Drugs.AddRange(newDrugs);
                await _context.SaveChangesAsync();
            }
            foreach (var item in newScriptsDrugs)
            {
                if (drugDict.TryGetValue(item.Item1, out var drug))
                {
                    foreach (var classInfoId in item.Item2)
                    {
                        if (!drugClassDict.TryGetValue(new { ClassId = classInfoId, DrugId = drug.Id }, out var drugClass))
                        {
                            newDrugClasses.Add(drugClass);
                        }
                    }
                }
            }
            if (newDrugClasses.Any())
            {
                _context.DrugClasses.AddRange(newDrugClasses);
                await _context.SaveChangesAsync();
            }
            // ========================================================
            // PHASE 2: Process Intermediate Dependents – DrugInsurance & ClassInsurance
            // ========================================================
            // ========================================================
            // PHASE 2: Process Intermediate Dependents – DrugInsurance & ClassInsurance
            // ========================================================

            // --- Load existing DrugInsurance records and build a dictionary keyed by (InsuranceId, DrugId)
            var existingDrugInsurances = await _context.DrugInsurances.ToListAsync();
            var diDict = existingDrugInsurances
                .ToDictionary(di => (di.InsuranceId, di.DrugId, di.BranchId));
            var branchDict = await _context.Branches.ToDictionaryAsync(b => b.Code);

            var existingClassInsurances = await _context.ClassInsurances.ToListAsync();

            var ciDict = existingClassInsurances
                .ToDictionary(ci => (ci.InsuranceId, ci.ClassInfoId, ci.Date.Year, ci.Date.Month, ci.BranchId));


            var drugBranchDict = await _context.DrugBranches.ToDictionaryAsync(g => (g.BranchId, g.DrugNDC));
            var newDrugInsurances = new List<DrugInsurance>();
            var newClassInsurances = new List<ClassInsurance>();


            foreach (var record in processedRecords)
            {
                if (!insuranceDict.TryGetValue(record.Bin, out var insurance))
                    continue;

                if (!insurancePCNDict.ContainsKey(record.PCN))
                {
                    var ins = new InsurancePCN { PCN = record.PCN, InsuranceId = insurance.Id };
                    newInsurancePCNs.Add(ins);
                    insurancePCNDict[record.PCN] = ins;
                }
            }
            if (newInsurancePCNs.Any())
            {
                _context.InsurancePCNs.AddRange(newInsurancePCNs);
                await _context.SaveChangesAsync();
            }
            foreach (var record in processedRecords)
            {
                if (!insurancePCNDict.TryGetValue(record.PCN, out var insurancePCN))
                    continue;

                if (!insuranceRxDict.ContainsKey(record.RxGroup))
                {
                    var ins = new InsuranceRx { RxGroup = record.RxGroup, InsurancePCNId = insurancePCN.Id };
                    newInsuranceRxes.Add(ins);
                    insuranceRxDict[record.RxGroup] = ins;
                }
            }
            if (newInsuranceRxes.Any())
            {
                _context.InsuranceRxes.AddRange(newInsuranceRxes);
                await _context.SaveChangesAsync();
            }
            var drugIds = await _context.Drugs.Select(d => d.Id).ToListAsync();
            var allDrugClasses = await _context.DrugClasses
                .Where(dc => drugIds.Contains(dc.DrugId))
                .ToListAsync();

            // Group them by DrugId
            var drugClassMap = allDrugClasses
                .GroupBy(dc => dc.DrugId)
                .ToDictionary(g => g.Key, g => g.ToList());
            foreach (var record in processedRecords)
            {
                decimal qty = 1;
                decimal realQTY = 1;
                record.RemainingStock = new Random().Next(10, 101);
                if (record.Quantity != "tableCell29")
                {
                    realQTY = decimal.Parse(record.Quantity);
                }

                // Normalize NDC and parse the date.
                record.NDCCode = NormalizeNdcTo11Digits(record.NDCCode);
                DateTime recordDate = DateTime.ParseExact(record.Date, "MM-dd-yy", CultureInfo.InvariantCulture)
                                                    .ToUniversalTime();

                decimal netValue = record.PatientPayment / qty + record.InsurancePayment / qty - record.AcquisitionCost / qty;
                // Use the first day of the month for ClassInsurance.
                DateTime yearMonth = new DateTime(recordDate.Year, recordDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);

                // Look up principal entities (guaranteed from Phase 1).
                if (!insuranceRxDict.TryGetValue(record.RxGroup, out var insuranceItem))
                {
                    Console.WriteLine("hoooooooooooooo");
                    continue;
                }
                if (!drugDict.TryGetValue(record.NDCCode, out var drug))
                    continue;

                var classInfoIds = drugClassMap.ContainsKey(drug.Id) ? drugClassMap[drug.Id] : new List<DrugClass>();

                // Only process if there are valid classInfoIds


                // -----------------------
                // Merge DrugInsurance
                // ------------------
                if (!branchDict.TryGetValue(record.Branch, out var branch))
                    continue;

                var diKey = (insuranceItem.Id, drug.Id, branch.Id);
                if (diDict.TryGetValue(diKey, out var existingDI))
                {
                    if (existingDI.Date < recordDate)
                    {
                        existingDI.Net = netValue;
                        existingDI.Quantity = realQTY;
                        existingDI.AcquisitionCost = record.AcquisitionCost;
                        existingDI.Discount = record.Discount;
                        existingDI.InsurancePayment = record.InsurancePayment;
                        existingDI.PatientPayment = record.PatientPayment;
                        existingDI.Date = recordDate;
                        existingDI.ScriptCode = record.Script;
                    }
                }
                else
                {
                    var newDI = new DrugInsurance
                    {
                        InsuranceId = insuranceItem.Id,
                        DrugId = drug.Id,
                        BranchId = branch.Id,
                        NDCCode = record.NDCCode,
                        Net = netValue,
                        ScriptCode = record.Script,
                        Date = recordDate,
                        Prescriber = record.Prescriber,
                        Quantity = realQTY,
                        AcquisitionCost = record.AcquisitionCost,
                        Discount = record.Discount,
                        InsurancePayment = record.InsurancePayment,
                        PatientPayment = record.PatientPayment,
                    };
                    newDrugInsurances.Add(newDI);
                    diDict.Add(diKey, newDI);
                }

                // -----------------------
                // Merge ClassInsurance
                // -----------------------
                foreach (var classInfoId in classInfoIds)
                {
                    var ciKey = (insuranceItem.Id, classInfoId.ClassId, recordDate.Year, recordDate.Month, branch.Id);
                    if (ciDict.TryGetValue(ciKey, out var existingCI))
                    {
                        // Update if this record has a higher net value.
                        if (netValue > existingCI.BestNet)
                        {
                            existingCI.BestNet = netValue / realQTY;
                            existingCI.BestACQ = record.AcquisitionCost / realQTY;
                            existingCI.BestInsurancePayment = record.InsurancePayment / realQTY;
                            existingCI.BestPatientPayment = record.PatientPayment / realQTY;
                            existingCI.DrugId = drug.Id;
                            existingCI.Qty = realQTY;
                            existingCI.ScriptCode = record.Script;
                            existingCI.ScriptDateTime = recordDate;
                        }
                    }
                    else
                    {
                        var newCI = new ClassInsurance
                        {
                            InsuranceId = insuranceItem.Id,
                            InsuranceName = insuranceItem.RxGroup,
                            ClassInfoId = classInfoId.ClassId,
                            DrugId = drug.Id,
                            BranchId = branch.Id,
                            Date = yearMonth,
                            ScriptDateTime = yearMonth,
                            ScriptCode = record.Script,
                            BestNet = netValue / realQTY,
                            BestACQ = record.AcquisitionCost / realQTY,
                            BestInsurancePayment = record.InsurancePayment / realQTY,
                            BestPatientPayment = record.PatientPayment / realQTY,
                            Qty = realQTY,
                        };
                        newClassInsurances.Add(newCI);
                        ciDict.Add(ciKey, newCI);
                    }
                }


            }

            // Now add only the new DrugInsurance and ClassInsurance records.
            _context.DrugInsurances.AddRange(newDrugInsurances);
            await _context.SaveChangesAsync();

            _context.ClassInsurances.AddRange(newClassInsurances);
            await _context.SaveChangesAsync();

            // ========================================================
            // PHASE 3: Process Users and Scripts
            // ========================================================
            // Preload Users, Branches, and Scripts.
            var userDict = await _context.Users
                .GroupBy(u => u.Email)
                .Select(g => g.First())
                .ToDictionaryAsync(u => u.Email);
            var scriptDict = await _context.Scripts.ToDictionaryAsync(s => s.ScriptCode);

            var newUsers = new List<User>();
            var newScripts = new List<Script>();
            var newDrugBranches = new List<DrugBranch>();
            // Process missing Users (record owner and prescriber).
            foreach (var record in processedRecords)
            {
                if (!branchDict.TryGetValue(record.Branch, out var branch))
                    continue;
                if (!drugDict.TryGetValue(record.NDCCode, out var drug))
                    continue;
                var tempkey = (branch.Id, drug.NDC);
                if (!drugBranchDict.TryGetValue(tempkey, out var drugBranch))
                {
                    var newDrugBranch = new DrugBranch
                    {
                        BranchId = branch.Id,
                        DrugNDC = drug.NDC,
                        Stock = record.RemainingStock
                    };
                    newDrugBranches.Add(newDrugBranch);
                    drugBranchDict.Add(tempkey, newDrugBranch);
                }
                if (!userDict.ContainsKey(record.User))
                {
                    var newUser = new User { ShortName = record.User, Name = record.User, Email = $"{record.User}@pharmacy.com", Password = BCrypt.Net.BCrypt.HashPassword("DefaultPass123"), BranchId = branch.Id };
                    newUsers.Add(newUser);
                    userDict[record.User] = newUser;
                }
                if (!userDict.ContainsKey(record.Prescriber))
                {
                    var newPrescriber = new User { ShortName = record.Prescriber, Name = record.Prescriber, Email = $"{record.Prescriber}@pharmacy.com", Password = BCrypt.Net.BCrypt.HashPassword("DefaultPass123"), BranchId = branch.Id };
                    newUsers.Add(newPrescriber);
                    userDict[record.Prescriber] = newPrescriber;
                }
            }
            if (newUsers.Any())
            {
                _context.Users.AddRange(newUsers);
                await _context.SaveChangesAsync();
            }
            if (newDrugBranches.Any())
            {
                _context.DrugBranches.AddRange(newDrugBranches);
                await _context.SaveChangesAsync();
            }

            // Process Scripts.
            foreach (var record in processedRecords)
            {
                DateTime recordDate = DateTime.ParseExact(record.Date, "MM-dd-yy", CultureInfo.InvariantCulture)
                                                .ToUniversalTime();
                if (!scriptDict.ContainsKey(record.Script))
                {
                    if (!branchDict.TryGetValue(record.Branch, out var branch))
                        continue;
                    // Use the record owner from userDict.
                    var owner = userDict[record.User];
                    var newScript = new Script
                    {
                        Date = recordDate,
                        ScriptCode = record.Script,
                        BranchId = branch.Id,
                        UserId = owner.Id
                    };
                    newScripts.Add(newScript);
                    scriptDict[record.Script] = newScript;
                }
            }
            if (newScripts.Any())
            {
                _context.Scripts.AddRange(newScripts);
                await _context.SaveChangesAsync();
            }

            // ========================================================
            // PHASE 4: Process ScriptItems
            // ========================================================
            // Build a temporary dictionary keyed by (ScriptId, DrugId)
            var scriptItemDic = await _context.ScriptItems
                .GroupBy(s => new { s.ScriptId, s.DrugId, s.Script.Date })
                .ToDictionaryAsync(g => g.Key, g => g.First());
            var tempScriptItems = new Dictionary<(int scriptId, int drugId, DateTime date), ScriptItem>();
            Console.WriteLine("Script item length before  : " + scriptItemDic.Count());

            foreach (var record in processedRecords)
            {
                record.NDCCode = NormalizeNdcTo11Digits(record.NDCCode);
                DateTime recordDate = DateTime.ParseExact(record.Date, "MM-dd-yy", CultureInfo.InvariantCulture)
                                                .ToUniversalTime();

                if (!insuranceRxDict.TryGetValue(record.RxGroup, out var insurance2))
                    continue;
                if (!drugDict.TryGetValue(record.NDCCode, out var drug2))
                    continue;
                if (!scriptDict.TryGetValue(record.Script, out var script))
                    continue;
                decimal realQTY = 1;
                record.RemainingStock = new Random().Next(10, 101);
                if (record.Quantity != "tableCell29")
                {
                    realQTY = decimal.Parse(record.Quantity);
                }

                var siKey = (script.Id, drug2.Id, recordDate);
                var siKey2 = new { ScriptId = script.Id, DrugId = drug2.Id, Date = recordDate };

                if (scriptItemDic.TryGetValue(siKey2, out var existingSI))
                {
                    continue;
                }
                else
                {
                    if (!userDict.TryGetValue(record.Prescriber, out var prescriber))
                        continue;
                    var newSI = new ScriptItem
                    {
                        ScriptId = script.Id,
                        DrugId = drug2.Id,
                        InsuranceId = insurance2.Id,
                        RxNumber = record.RxNumber,
                        UserEmail = prescriber.Email,
                        PF = record.PF,
                        Quantity = realQTY,
                        RemainingStock = record.RemainingStock,
                        AcquisitionCost = record.AcquisitionCost,
                        Discount = record.Discount,
                        InsurancePayment = record.InsurancePayment,
                        PatientPayment = record.PatientPayment,
                        NDCCode = record.NDCCode
                    };

                    tempScriptItems.Add(siKey, newSI);
                    scriptItemDic[siKey2] = newSI;
                }
            }

            _context.ScriptItems.AddRange(tempScriptItems.Values);
            await _context.SaveChangesAsync();
            return tempScriptItems.Count();
        }




        public async Task ImportDrugInsuranceAsync(string filePath = "Scripts22-7-2025.csv")
        {
            // ========================================================
            // PHASE 0: Read CSV Records
            // ========================================================
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                HeaderValidated = null,
                MissingFieldFound = null,
            };

            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, config);
            var records = csv.GetRecords<ScriptRecord>().ToList();

            // Keep only records that yield a valid Drug.
            var processedRecords = new List<ScriptRecord>();

            // ========================================================
            // PHASE 1: Process Principal Entities – Insurances & Drugs
            // ========================================================
            // Preload existing Insurances, Drugs, and DrugClasses.
            var insuranceDict = await _context.Insurances.ToDictionaryAsync(i => i.Bin);
            var insurancePCNDict = await _context.InsurancePCNs.ToDictionaryAsync(i => i.PCN);
            var insuranceRxDict = await _context.InsuranceRxes.ToDictionaryAsync(i => i.RxGroup);
            var drugsFromDb = await _context.Drugs.ToListAsync();
            var drugDict = drugsFromDb
                                    .GroupBy(d => d.NDC)
                                    .ToDictionary(g => g.Key, g => g.First());
            var classInfoDict = await _context.ClassInfos.ToDictionaryAsync(ci => ci.Id);
            var drugByNameDict = drugsFromDb
                                    .GroupBy(d => d.Name)
                                    .ToDictionary(g => g.Key, g => g.First());
            var drugClassDict = await _context.DrugClasses.ToDictionaryAsync(dc => new { dc.ClassId, dc.DrugId });
            var newInsurances = new List<Insurance>();
            var newInsurancePCNs = new List<InsurancePCN>();
            var newInsuranceRxes = new List<InsuranceRx>();

            var newDrugs = new List<Drug>();
            var newDrugClasses = new List<DrugClass>();
            int batchSize = 1000, countPhase1 = 0;
            var newScriptsDrugs = new List<(string, List<int>)>();

            foreach (var record in records)
            {
                record.Bin = record.Bin.ToUpper();
                record.PCN = record.PCN.ToUpper();
                record.RxGroup = record.RxGroup.ToUpper();
                record.DrugName = record.DrugName.ToUpper();
                if (record.Bin.Length < 6)
                {
                    record.Bin = record.Bin.PadLeft(6, '0');
                }
                if (record.PCN.Length < 1)
                {
                    record.PCN = record.Bin + "(Other)";
                }
                if (record.RxGroup.Length < 1)
                {

                    record.RxGroup = record.PCN + "(Other)";

                }
                record.RxGroup = record.RxGroup.Trim();
                // Normalize NDC.
                record.NDCCode = NormalizeNdcTo11Digits(record.NDCCode);
                if (string.IsNullOrWhiteSpace(record.NDCCode) || record.NDCCode == "00000000000")
                {
                    Console.WriteLine($"Skipping record with invalid NDC: {record.NDCCode}");
                    continue;
                }
                // ---- Process Insurance
                if (!insuranceDict.ContainsKey(record.Bin))
                {
                    var ins = new Insurance { Bin = record.Bin };
                    newInsurances.Add(ins);
                    insuranceDict[record.Bin] = ins; // will have generated Id after saving
                }
                // ---- Process Drug
                Drug drug = null;
                if (!drugDict.TryGetValue(record.NDCCode, out drug))
                {
                    if (drugByNameDict.TryGetValue(record.DrugName, out var tempDrug))
                    {
                        drug = new Drug
                        {
                            Name = record.DrugName,
                            NDC = record.NDCCode,
                            Form = tempDrug.Form,
                            Strength = tempDrug.Strength,
                            ACQ = record.AcquisitionCost,
                            AWP = 0,
                            Rxcui = tempDrug.Rxcui,
                            Route = tempDrug.Route,
                            Ingrdient = tempDrug.Ingrdient,
                            TECode = tempDrug.TECode,
                            ApplicationNumber = tempDrug.ApplicationNumber,
                            ApplicationType = tempDrug.ApplicationType
                        };
                        var classInfos = await _context.DrugClasses.Where(x => x.DrugId == tempDrug.Id).Select(x => x.ClassId).ToListAsync();
                        newDrugs.Add(drug);
                        newScriptsDrugs.Add((drug.NDC, classInfos));
                        drugDict[record.NDCCode] = drug;
                    }
                    else
                    {
                        Console.WriteLine($"Skipping record: Drug with NDC {record.NDCCode} not found.");
                        continue;
                    }
                }
                processedRecords.Add(record);
                countPhase1++;
                if (countPhase1 % batchSize == 0)
                {
                    if (newInsurances.Any())
                    {
                        _context.Insurances.AddRange(newInsurances);
                        await _context.SaveChangesAsync();
                        newInsurances.Clear();
                    }
                    if (newDrugs.Any())
                    {
                        _context.Drugs.AddRange(newDrugs);
                        await _context.SaveChangesAsync();
                        newDrugs.Clear();
                    }
                }
            }


            if (newInsurances.Any())
            {
                _context.Insurances.AddRange(newInsurances);
                await _context.SaveChangesAsync();
            }
            if (newDrugs.Any())
            {
                _context.Drugs.AddRange(newDrugs);
                await _context.SaveChangesAsync();
            }
            foreach (var item in newScriptsDrugs)
            {
                if (drugDict.TryGetValue(item.Item1, out var drug))
                {
                    foreach (var classInfoId in item.Item2)
                    {
                        if (!drugClassDict.TryGetValue(new { ClassId = classInfoId, DrugId = drug.Id }, out var drugClass))
                        {
                            newDrugClasses.Add(drugClass);
                        }
                    }
                }
            }
            if (newDrugClasses.Any())
            {
                _context.DrugClasses.AddRange(newDrugClasses);
                await _context.SaveChangesAsync();
            }
            // ========================================================
            // PHASE 2: Process Intermediate Dependents – DrugInsurance & ClassInsurance
            // ========================================================
            // ========================================================
            // PHASE 2: Process Intermediate Dependents – DrugInsurance & ClassInsurance
            // ========================================================

            // --- Load existing DrugInsurance records and build a dictionary keyed by (InsuranceId, DrugId)
            var existingDrugInsurances = await _context.DrugInsurances.ToListAsync();
            var diDict = existingDrugInsurances
                .ToDictionary(di => (di.InsuranceId, di.DrugId, di.BranchId));
            var branchDict = await _context.Branches.ToDictionaryAsync(b => b.Code);

            var existingClassInsurances = await _context.ClassInsurances.ToListAsync();

            var ciDict = existingClassInsurances
                .ToDictionary(ci => (ci.InsuranceId, ci.ClassInfoId, ci.Date.Year, ci.Date.Month, ci.BranchId));


            var drugBranchDict = await _context.DrugBranches.ToDictionaryAsync(g => (g.BranchId, g.DrugNDC));
            var newDrugInsurances = new List<DrugInsurance>();
            var newClassInsurances = new List<ClassInsurance>();


            foreach (var record in processedRecords)
            {
                if (!insuranceDict.TryGetValue(record.Bin, out var insurance))
                    continue;

                if (!insurancePCNDict.ContainsKey(record.PCN))
                {
                    var ins = new InsurancePCN { PCN = record.PCN, InsuranceId = insurance.Id };
                    newInsurancePCNs.Add(ins);
                    insurancePCNDict[record.PCN] = ins;
                }
            }
            if (newInsurancePCNs.Any())
            {
                _context.InsurancePCNs.AddRange(newInsurancePCNs);
                await _context.SaveChangesAsync();
            }
            foreach (var record in processedRecords)
            {
                if (!insurancePCNDict.TryGetValue(record.PCN, out var insurancePCN))
                    continue;

                if (!insuranceRxDict.ContainsKey(record.RxGroup))
                {
                    var ins = new InsuranceRx { RxGroup = record.RxGroup, InsurancePCNId = insurancePCN.Id };
                    newInsuranceRxes.Add(ins);
                    insuranceRxDict[record.RxGroup] = ins;
                }
            }
            if (newInsuranceRxes.Any())
            {
                _context.InsuranceRxes.AddRange(newInsuranceRxes);
                await _context.SaveChangesAsync();
            }
            var drugIds = await _context.Drugs.Select(d => d.Id).ToListAsync();
            var allDrugClasses = await _context.DrugClasses
                .Where(dc => drugIds.Contains(dc.DrugId))
                .ToListAsync();

            // Group them by DrugId
            var drugClassMap = allDrugClasses
                .GroupBy(dc => dc.DrugId)
                .ToDictionary(g => g.Key, g => g.ToList());
            foreach (var record in processedRecords)
            {
                decimal qty = 1;
                decimal realQTY = 1;
                record.RemainingStock = new Random().Next(10, 101);
                if (record.Quantity != "tableCell29")
                {
                    realQTY = decimal.Parse(record.Quantity);
                }

                // Normalize NDC and parse the date.
                record.NDCCode = NormalizeNdcTo11Digits(record.NDCCode);
                DateTime recordDate = DateTime.ParseExact(record.Date, "MM-dd-yy", CultureInfo.InvariantCulture)
                                                    .ToUniversalTime();
                decimal netValue = record.PatientPayment / qty + record.InsurancePayment / qty - record.AcquisitionCost / qty;
                // Use the first day of the month for ClassInsurance.
                DateTime yearMonth = new DateTime(recordDate.Year, recordDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);

                // Look up principal entities (guaranteed from Phase 1).
                if (!insuranceRxDict.TryGetValue(record.RxGroup, out var insuranceItem))
                {
                    Console.WriteLine("hoooooooooooooo");
                    continue;
                }
                if (!drugDict.TryGetValue(record.NDCCode, out var drug))
                    continue;

                var classInfoIds = drugClassMap.ContainsKey(drug.Id) ? drugClassMap[drug.Id] : new List<DrugClass>();

                // Only process if there are valid classInfoIds


                // -----------------------
                // Merge DrugInsurance
                // ------------------
                if (!branchDict.TryGetValue(record.Branch, out var branch))
                    continue;

                var diKey = (insuranceItem.Id, drug.Id, branch.Id);
                if (diDict.TryGetValue(diKey, out var existingDI))
                {
                    if (existingDI.Date < recordDate)
                    {
                        existingDI.Net = netValue;
                        existingDI.Quantity = realQTY;
                        existingDI.AcquisitionCost = record.AcquisitionCost;
                        existingDI.Discount = record.Discount;
                        existingDI.InsurancePayment = record.InsurancePayment;
                        existingDI.PatientPayment = record.PatientPayment;
                        existingDI.Date = recordDate;
                        existingDI.ScriptCode = record.Script;
                    }
                }
                else
                {
                    var newDI = new DrugInsurance
                    {
                        InsuranceId = insuranceItem.Id,
                        DrugId = drug.Id,
                        BranchId = branch.Id,
                        NDCCode = record.NDCCode,
                        Net = netValue,
                        ScriptCode = record.Script,
                        Date = recordDate,
                        Prescriber = record.Prescriber,
                        Quantity = realQTY,
                        AcquisitionCost = record.AcquisitionCost,
                        Discount = record.Discount,
                        InsurancePayment = record.InsurancePayment,
                        PatientPayment = record.PatientPayment,
                    };
                    newDrugInsurances.Add(newDI);
                    diDict.Add(diKey, newDI);
                }

                // -----------------------
                // Merge ClassInsurance
                // -----------------------
                foreach (var classInfoId in classInfoIds)
                {
                    var ciKey = (insuranceItem.Id, classInfoId.ClassId, recordDate.Year, recordDate.Month, branch.Id);
                    if (ciDict.TryGetValue(ciKey, out var existingCI))
                    {
                        // Update if this record has a higher net value.
                        if (netValue > existingCI.BestNet)
                        {
                            existingCI.BestNet = netValue / realQTY;
                            existingCI.BestACQ = record.AcquisitionCost / realQTY;
                            existingCI.BestInsurancePayment = record.InsurancePayment / realQTY;
                            existingCI.BestPatientPayment = record.PatientPayment / realQTY;
                            existingCI.DrugId = drug.Id;
                            existingCI.Qty = realQTY;
                            existingCI.ScriptCode = record.Script;
                            existingCI.ScriptDateTime = recordDate;
                        }
                    }
                    else
                    {
                        var newCI = new ClassInsurance
                        {
                            InsuranceId = insuranceItem.Id,
                            InsuranceName = insuranceItem.RxGroup,
                            ClassInfoId = classInfoId.ClassId,
                            DrugId = drug.Id,
                            BranchId = branch.Id,
                            Date = yearMonth,
                            ScriptDateTime = yearMonth,
                            ScriptCode = record.Script,
                            BestNet = netValue / realQTY,
                            BestACQ = record.AcquisitionCost / realQTY,
                            BestInsurancePayment = record.InsurancePayment / realQTY,
                            BestPatientPayment = record.PatientPayment / realQTY,
                            Qty = realQTY,
                        };
                        newClassInsurances.Add(newCI);
                        ciDict.Add(ciKey, newCI);
                    }
                }


            }

            // Now add only the new DrugInsurance and ClassInsurance records.
            _context.DrugInsurances.AddRange(newDrugInsurances);
            await _context.SaveChangesAsync();

            _context.ClassInsurances.AddRange(newClassInsurances);
            await _context.SaveChangesAsync();

            // ========================================================
            // PHASE 3: Process Users and Scripts
            // ========================================================
            // Preload Users, Branches, and Scripts.
            var userDict = await _context.Users
                .GroupBy(u => u.Email)
                .Select(g => g.First())
                .ToDictionaryAsync(u => u.Email);
            var scriptDict = await _context.Scripts.ToDictionaryAsync(s => s.ScriptCode);

            var newUsers = new List<User>();
            var newScripts = new List<Script>();
            var newDrugBranches = new List<DrugBranch>();
            // Process missing Users (record owner and prescriber).
            foreach (var record in processedRecords)
            {
                if (!branchDict.TryGetValue(record.Branch, out var branch))
                    continue;
                if (!drugDict.TryGetValue(record.NDCCode, out var drug))
                    continue;
                var tempkey = (branch.Id, drug.NDC);
                if (!drugBranchDict.TryGetValue(tempkey, out var drugBranch))
                {
                    var newDrugBranch = new DrugBranch
                    {
                        BranchId = branch.Id,
                        DrugNDC = drug.NDC,
                        Stock = record.RemainingStock
                    };
                    newDrugBranches.Add(newDrugBranch);
                    drugBranchDict.Add(tempkey, newDrugBranch);
                }
                if (!userDict.ContainsKey(record.User))
                {
                    var newUser = new User { ShortName = record.User, Name = record.User, Email = $"{record.User}@pharmacy.com", Password = BCrypt.Net.BCrypt.HashPassword("DefaultPass123"), BranchId = branch.Id };
                    newUsers.Add(newUser);
                    userDict[record.User] = newUser;
                }
                if (!userDict.ContainsKey(record.Prescriber))
                {
                    var newPrescriber = new User { ShortName = record.Prescriber, Name = record.Prescriber, Email = $"{record.Prescriber}@pharmacy.com", Password = BCrypt.Net.BCrypt.HashPassword("DefaultPass123"), BranchId = branch.Id };
                    newUsers.Add(newPrescriber);
                    userDict[record.Prescriber] = newPrescriber;
                }
            }
            if (newUsers.Any())
            {
                _context.Users.AddRange(newUsers);
                await _context.SaveChangesAsync();
            }
            if (newDrugBranches.Any())
            {
                _context.DrugBranches.AddRange(newDrugBranches);
                await _context.SaveChangesAsync();
            }

            // Process Scripts.
            foreach (var record in processedRecords)
            {
                DateTime recordDate = DateTime.ParseExact(record.Date, "MM-dd-yy", CultureInfo.InvariantCulture)
                                                .ToUniversalTime();
                if (!scriptDict.ContainsKey(record.Script))
                {
                    if (!branchDict.TryGetValue(record.Branch, out var branch))
                        continue;
                    // Use the record owner from userDict.
                    var owner = userDict[record.User];
                    var newScript = new Script
                    {
                        Date = recordDate,
                        ScriptCode = record.Script,
                        BranchId = branch.Id,
                        UserId = owner.Id
                    };
                    newScripts.Add(newScript);
                    scriptDict[record.Script] = newScript;
                }
            }
            if (newScripts.Any())
            {
                _context.Scripts.AddRange(newScripts);
                await _context.SaveChangesAsync();
            }

            // ========================================================
            // PHASE 4: Process ScriptItems
            // ========================================================
            // Build a temporary dictionary keyed by (ScriptId, DrugId)
            var tempScriptItems = new Dictionary<(int scriptId, int drugId), ScriptItem>();
            foreach (var record in processedRecords)
            {
                record.NDCCode = NormalizeNdcTo11Digits(record.NDCCode);
                DateTime recordDate = DateTime.ParseExact(record.Date, "MM-dd-yy", CultureInfo.InvariantCulture)
                                                .ToUniversalTime();

                if (!insuranceRxDict.TryGetValue(record.RxGroup, out var insurance2))
                    continue;
                if (!drugDict.TryGetValue(record.NDCCode, out var drug2))
                    continue;
                if (!scriptDict.TryGetValue(record.Script, out var script))
                    continue;
                decimal realQTY = 1;
                record.RemainingStock = new Random().Next(10, 101);
                if (record.Quantity != "tableCell29")
                {
                    realQTY = decimal.Parse(record.Quantity);
                }
                var siKey = (script.Id, drug2.Id);
                if (tempScriptItems.TryGetValue(siKey, out var existingSI))
                {
                    if (script.Date <= recordDate)
                    {
                        existingSI.AcquisitionCost = record.AcquisitionCost;
                        existingSI.Discount = record.Discount;
                        existingSI.InsurancePayment = record.InsurancePayment;
                        existingSI.PatientPayment = record.PatientPayment;
                    }
                }
                else
                {
                    if (!userDict.TryGetValue(record.Prescriber, out var prescriber))
                        continue;
                    var newSI = new ScriptItem
                    {
                        ScriptId = script.Id,
                        DrugId = drug2.Id,
                        InsuranceId = insurance2.Id,
                        RxNumber = record.RxNumber,
                        UserEmail = prescriber.Email,
                        PF = record.PF,
                        Quantity = realQTY,
                        RemainingStock = record.RemainingStock,
                        AcquisitionCost = record.AcquisitionCost,
                        Discount = record.Discount,
                        InsurancePayment = record.InsurancePayment,
                        PatientPayment = record.PatientPayment,
                        NDCCode = record.NDCCode
                    };
                    tempScriptItems.Add(siKey, newSI);
                }
            }
            _context.ScriptItems.AddRange(tempScriptItems.Values);
            await _context.SaveChangesAsync();
        }
        public async Task<int> ImportDrugInsuranceFileAsync(IFormFile uploadedFile, CancellationToken ct = default)
        {
            if (uploadedFile == null || uploadedFile.Length == 0)
                throw new ArgumentException("Uploaded file is empty or missing.", nameof(uploadedFile));

            // ========================================================
            // PHASE 0: Read CSV directly from the uploaded stream
            // ========================================================
            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                HeaderValidated = null,
                MissingFieldFound = null,
                BadDataFound = null
            };

            List<ScriptRecord> records;

            using (var stream = uploadedFile.OpenReadStream())
            using (var reader = new StreamReader(
                stream,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true,
                bufferSize: 1024,
                leaveOpen: false))
            using (var csv = new CsvReader(reader, csvConfig))
            {
                records = csv.GetRecords<ScriptRecord>().ToList();
            }

            var processedRecords = new List<ScriptRecord>();
            var ci = StringComparer.OrdinalIgnoreCase;

            // ── Accepted date formats from client CSVs ──
            var dateFormats = new[]
            {
        "yyyy-MM-dd",
        "MM-dd-yy",
        "MM/dd/yyyy",
        "M/d/yyyy",
        "yyyy-MM-dd HH:mm:ss",
        "MM/dd/yy"
    };

            // ── Helper: parse any of the supported date formats ──
            bool TryParseRecordDate(string raw, out DateTime parsed)
            {
                parsed = default;

                if (string.IsNullOrWhiteSpace(raw))
                    return false;

                if (DateTime.TryParseExact(raw.Trim(), dateFormats, CultureInfo.InvariantCulture,
                                           DateTimeStyles.None, out parsed))
                    return true;

                return DateTime.TryParse(raw.Trim(), CultureInfo.InvariantCulture,
                                         DateTimeStyles.None, out parsed);
            }

            // ========================================================
            // LOAD ALL DATA UPFRONT
            // ========================================================
            var insuranceDict = (await _context.Insurances.AsNoTracking().ToListAsync(ct))
                .GroupBy(i => (i.Bin ?? "").Trim(), ci)
                .ToDictionary(g => g.Key, g => g.First(), ci);

            var insurancePCNDict = (await _context.InsurancePCNs.AsNoTracking().ToListAsync(ct))
                .GroupBy(p => (p.PCN ?? "").Trim(), ci)
                .ToDictionary(g => g.Key, g => g.First(), ci);

            var insuranceRxDict = (await _context.InsuranceRxes.AsNoTracking().ToListAsync(ct))
                .GroupBy(r => (r.RxGroup ?? "").Trim(), ci)
                .ToDictionary(g => g.Key, g => g.First(), ci);

            var drugsFromDb = await _context.Drugs.AsNoTracking().ToListAsync(ct);

            var drugDict = drugsFromDb
                .Where(d => !string.IsNullOrWhiteSpace(d.NDC))
                .GroupBy(d => d.NDC)
                .ToDictionary(g => g.Key, g => g.First());

            var drugByNameDict = drugsFromDb
                .Where(d => !string.IsNullOrWhiteSpace(d.Name))
                .GroupBy(d => d.Name)
                .ToDictionary(g => g.Key, g => g.First());

            var existingDrugClasses = await _context.DrugClasses.AsNoTracking().ToListAsync(ct);

            var drugClassKeySet = new HashSet<(int ClassId, int DrugId)>(
                existingDrugClasses.Select(dc => (dc.ClassId, dc.DrugId))
            );

            var branchDict = await _context.Branches
                .AsNoTracking()
                .ToDictionaryAsync(b => b.Code, ct);

            var userDict = (await _context.Users.AsNoTracking().ToListAsync(ct))
                .Where(u => !string.IsNullOrWhiteSpace(u.Email))
                .GroupBy(u => u.Email)
                .ToDictionary(g => g.Key, g => g.First());

            // IMPORTANT:
            // ScriptCode alone is not unique.
            // Same ScriptCode can exist in different branches.
            var scriptDict = (await _context.Scripts
                .AsNoTracking()
                .ToListAsync(ct))
                .GroupBy(s => (s.ScriptCode, s.BranchId))
                .ToDictionary(g => g.Key, g => g.First());

            var drugBranchDict = await _context.DrugBranches
                .AsNoTracking()
                .ToDictionaryAsync(g => (g.BranchId, g.DrugNDC), ct);

            // ========================================================
            // IMPORTANT FIX:
            // These two are updated later, so DO NOT use AsNoTracking().
            // EF must track them automatically.
            // ========================================================
            var existingDrugInsurances = await _context.DrugInsurances.ToListAsync(ct);

            var diDict = existingDrugInsurances
                .GroupBy(di => (di.InsuranceId, di.DrugId, di.BranchId))
                .ToDictionary(g => g.Key, g => g.First());

            var existingClassInsurances = await _context.ClassInsurances.ToListAsync(ct);

            var ciDict = existingClassInsurances
                .GroupBy(x => (x.InsuranceId, x.ClassInfoId, x.Date.Year, x.Date.Month, x.BranchId))
                .ToDictionary(g => g.Key, g => g.First());

            // ScriptItem uniqueness:
            // Same script can contain multiple items.
            // Use ScriptId + DrugId + RxNumber + ScriptDate.
            var scriptItemDic = (await _context.ScriptItems
                .AsNoTracking()
                .Select(s => new
                {
                    s.ScriptId,
                    s.DrugId,
                    s.RxNumber,
                    ScriptDate = s.Script.Date
                })
                .ToListAsync(ct))
                .GroupBy(s => (s.ScriptId, s.DrugId, s.RxNumber, s.ScriptDate))
                .ToDictionary(g => g.Key, g => true);

            // Build drug class map upfront
            var drugClassMap = existingDrugClasses
                .GroupBy(dc => dc.DrugId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // ========================================================
            // PHASE 1: Prepare collections for bulk insert
            // ========================================================
            var newInsurances = new List<Insurance>();
            var newInsurancePCNs = new List<InsurancePCN>();
            var newInsuranceRxes = new List<InsuranceRx>();
            var newDrugs = new List<Drug>();
            var stagedDrugClassLinks = new List<(string ndc, List<int> classInfoIds)>();

            foreach (var record in records)
            {
                record.Bin = (record.Bin ?? "").Trim().ToUpperInvariant();
                record.PCN = (record.PCN ?? "").Trim().ToUpperInvariant();
                record.RxGroup = (record.RxGroup ?? "").Trim().ToUpperInvariant();
                record.DrugName = (record.DrugName ?? "").Trim().ToUpperInvariant();
                record.Branch = (record.Branch ?? "").Trim();
                record.NDCCode = NormalizeNdcTo11Digits(record.NDCCode);

                // ── Normalize status fields ──
                record.Status = (record.Status ?? "").Trim();
                record.RxStatus = (record.RxStatus ?? "").Trim();
                record.Priority = (record.Priority ?? "").Trim();
                record.Unit = (record.Unit ?? "").Trim();

                if (record.Bin.Length < 6)
                    record.Bin = record.Bin.PadLeft(6, '0');

                if (string.IsNullOrWhiteSpace(record.PCN))
                    record.PCN = record.Bin + "(OTHER)";

                if (string.IsNullOrWhiteSpace(record.RxGroup))
                    record.RxGroup = record.PCN + "(OTHER)";

                if (string.IsNullOrWhiteSpace(record.NDCCode) || record.NDCCode == "00000000000")
                    continue;

                // Insurance by BIN
                if (!insuranceDict.ContainsKey(record.Bin))
                {
                    var ins = new Insurance
                    {
                        Bin = record.Bin
                    };

                    newInsurances.Add(ins);
                    insuranceDict[record.Bin] = ins;
                }

                // Drug by NDC
                if (!drugDict.TryGetValue(record.NDCCode, out var drug))
                {
                    if (drugByNameDict.TryGetValue(record.DrugName, out var template))
                    {
                        drug = new Drug
                        {
                            Name = record.DrugName,
                            NDC = record.NDCCode,
                            Form = template.Form,
                            Strength = template.Strength,
                            ACQ = record.AcquisitionCost,
                            // ── CHANGED: populate AWP from CSV if available, otherwise fall back to template, otherwise 0 ──
                            AWP = record.AWP ?? template.AWP,
                            Rxcui = template.Rxcui,
                            Route = template.Route,
                            Ingrdient = template.Ingrdient,
                            TECode = template.TECode,
                            ApplicationNumber = template.ApplicationNumber,
                            ApplicationType = template.ApplicationType
                        };

                        newDrugs.Add(drug);
                        drugDict[record.NDCCode] = drug;

                        if (drugClassMap.TryGetValue(template.Id, out var templateClasses))
                        {
                            stagedDrugClassLinks.Add((
                                drug.NDC,
                                templateClasses.Select(dc => dc.ClassId).ToList()
                            ));
                        }
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    // ── If drug exists but has no AWP and the CSV provides one, update it ──
                    if ((drug.AWP == 0) && record.AWP.HasValue && record.AWP.Value > 0)
                    {
                        drug.AWP = record.AWP.Value;
                        _context.Drugs.Update(drug);
                    }
                }

                processedRecords.Add(record);
            }

            // ========================================================
            // BULK INSERT PHASE 1
            // ========================================================
            if (newInsurances.Any())
            {
                _context.Insurances.AddRange(newInsurances);
                await _context.SaveChangesAsync(ct);

                var bins = newInsurances.Select(i => i.Bin).ToList();

                var freshInsurances = await _context.Insurances
                    .Where(i => bins.Contains(i.Bin))
                    .ToListAsync(ct);

                foreach (var ins in freshInsurances)
                    insuranceDict[ins.Bin] = ins;
            }

            if (newDrugs.Any())
            {
                _context.Drugs.AddRange(newDrugs);
                await _context.SaveChangesAsync(ct);

                var ndcs = newDrugs.Select(d => d.NDC).ToList();

                var freshDrugs = await _context.Drugs
                    .Where(d => ndcs.Contains(d.NDC))
                    .ToListAsync(ct);

                foreach (var d in freshDrugs)
                    drugDict[d.NDC] = d;
            }

            // Create DrugClass links
            var newDrugClasses = new List<DrugClass>();

            foreach (var (ndc, classIds) in stagedDrugClassLinks)
            {
                if (!drugDict.TryGetValue(ndc, out var d))
                    continue;

                foreach (var classId in classIds)
                {
                    var key = (classId, d.Id);

                    if (drugClassKeySet.Add(key))
                    {
                        var newDrugClass = new DrugClass
                        {
                            ClassId = classId,
                            DrugId = d.Id
                        };

                        newDrugClasses.Add(newDrugClass);

                        if (!drugClassMap.ContainsKey(d.Id))
                            drugClassMap[d.Id] = new List<DrugClass>();

                        drugClassMap[d.Id].Add(newDrugClass);
                    }
                }
            }

            if (newDrugClasses.Any())
            {
                _context.DrugClasses.AddRange(newDrugClasses);
                await _context.SaveChangesAsync(ct);
            }

            // ========================================================
            // PHASE 2: Intermediates - Batch process
            // ========================================================

            // Build PCNs
            foreach (var record in processedRecords)
            {
                if (!insuranceDict.TryGetValue(record.Bin, out var insurance))
                    continue;

                if (!insurancePCNDict.ContainsKey(record.PCN))
                {
                    var insPcn = new InsurancePCN
                    {
                        PCN = record.PCN,
                        InsuranceId = insurance.Id
                    };

                    newInsurancePCNs.Add(insPcn);
                    insurancePCNDict[record.PCN] = insPcn;
                }
            }

            if (newInsurancePCNs.Any())
            {
                _context.InsurancePCNs.AddRange(newInsurancePCNs);
                await _context.SaveChangesAsync(ct);

                var pcns = newInsurancePCNs.Select(p => p.PCN).ToList();

                var freshPCNs = await _context.InsurancePCNs
                    .Where(p => pcns.Contains(p.PCN))
                    .ToListAsync(ct);

                foreach (var pcn in freshPCNs)
                    insurancePCNDict[pcn.PCN] = pcn;
            }

            // Build RxGroups
            foreach (var record in processedRecords)
            {
                if (!insurancePCNDict.TryGetValue(record.PCN, out var insurancePCN))
                    continue;

                if (!insuranceRxDict.ContainsKey(record.RxGroup))
                {
                    var insRx = new InsuranceRx
                    {
                        RxGroup = record.RxGroup,
                        InsurancePCNId = insurancePCN.Id
                    };

                    newInsuranceRxes.Add(insRx);
                    insuranceRxDict[record.RxGroup] = insRx;
                }
            }

            if (newInsuranceRxes.Any())
            {
                _context.InsuranceRxes.AddRange(newInsuranceRxes);
                await _context.SaveChangesAsync(ct);

                var rxGroups = newInsuranceRxes.Select(r => r.RxGroup).ToList();

                var freshRxs = await _context.InsuranceRxes
                    .Where(r => rxGroups.Contains(r.RxGroup))
                    .ToListAsync(ct);

                foreach (var rx in freshRxs)
                    insuranceRxDict[rx.RxGroup] = rx;
            }

            // ========================================================
            // PHASE 2B: DrugInsurances and ClassInsurances
            // ========================================================
            var newDrugInsurances = new List<DrugInsurance>();
            var newClassInsurances = new List<ClassInsurance>();

            foreach (var record in processedRecords)
            {
                // ── Skip cancelled scripts for DrugInsurance/ClassInsurance ──
                // These tables drive the "best alternative" optimization logic,
                // and cancelled scripts should not influence that comparison.
                if (string.Equals(record.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
                    continue;

                decimal qty = 1;

                if (!string.Equals(record.Quantity, "tableCell29", StringComparison.OrdinalIgnoreCase)
                    && decimal.TryParse(record.Quantity, NumberStyles.Any, CultureInfo.InvariantCulture, out var q))
                {
                    qty = q == 0 ? 1 : q;
                }

                // ── CHANGED: multi-format date parsing ──
                if (!TryParseRecordDate(record.Date, out var rdLocal))
                    continue;

                var recordDate = DateTime.SpecifyKind(rdLocal, DateTimeKind.Unspecified);
                var recordDateUtc = DateTime.SpecifyKind(recordDate, DateTimeKind.Utc);

                var netTotal = (record.PatientPayment + record.InsurancePayment) - record.AcquisitionCost;
                var netPerUnit = netTotal / qty;

                var yearMonth = new DateTime(
                    recordDateUtc.Year,
                    recordDateUtc.Month,
                    1,
                    0,
                    0,
                    0,
                    DateTimeKind.Utc
                );

                if (!insuranceRxDict.TryGetValue(record.RxGroup, out var insuranceRx))
                    continue;

                if (!drugDict.TryGetValue(record.NDCCode, out var drug))
                    continue;

                if (!branchDict.TryGetValue(record.Branch, out var branch))
                    continue;

                // DrugInsurance
                var diKey = (insuranceRx.Id, drug.Id, branch.Id);

                if (diDict.TryGetValue(diKey, out var existingDI))
                {
                    if (existingDI.Date < recordDateUtc)
                    {
                        existingDI.Net = netTotal;
                        existingDI.Quantity = qty;
                        existingDI.AcquisitionCost = record.AcquisitionCost;
                        existingDI.Discount = record.Discount;
                        existingDI.InsurancePayment = record.InsurancePayment;
                        existingDI.PatientPayment = record.PatientPayment;
                        existingDI.Date = recordDateUtc;
                        existingDI.ScriptCode = record.Script;
                        existingDI.Prescriber = record.Prescriber;
                        existingDI.NDCCode = record.NDCCode;
                    }
                }
                else
                {
                    var di = new DrugInsurance
                    {
                        InsuranceId = insuranceRx.Id,
                        DrugId = drug.Id,
                        BranchId = branch.Id,
                        NDCCode = record.NDCCode,
                        Net = netTotal,
                        ScriptCode = record.Script,
                        Date = recordDateUtc,
                        Prescriber = record.Prescriber,
                        Quantity = qty,
                        AcquisitionCost = record.AcquisitionCost,
                        Discount = record.Discount,
                        InsurancePayment = record.InsurancePayment,
                        PatientPayment = record.PatientPayment
                    };

                    newDrugInsurances.Add(di);
                    diDict.Add(diKey, di);
                }

                // ClassInsurance
                if (drugClassMap.TryGetValue(drug.Id, out var classLinks))
                {
                    foreach (var link in classLinks)
                    {
                        var ciKey2 = (
                            insuranceRx.Id,
                            link.ClassId,
                            recordDateUtc.Year,
                            recordDateUtc.Month,
                            branch.Id
                        );

                        if (ciDict.TryGetValue(ciKey2, out var existingCI))
                        {
                            if (netPerUnit > existingCI.BestNet)
                            {
                                existingCI.BestNet = netPerUnit;
                                existingCI.BestACQ = record.AcquisitionCost / qty;
                                existingCI.BestInsurancePayment = record.InsurancePayment / qty;
                                existingCI.BestPatientPayment = record.PatientPayment / qty;
                                existingCI.DrugId = drug.Id;
                                existingCI.Qty = qty;
                                existingCI.ScriptCode = record.Script;
                                existingCI.ScriptDateTime = recordDateUtc;
                            }
                        }
                        else
                        {
                            var cii = new ClassInsurance
                            {
                                InsuranceId = insuranceRx.Id,
                                InsuranceName = insuranceRx.RxGroup,
                                ClassInfoId = link.ClassId,
                                DrugId = drug.Id,
                                BranchId = branch.Id,
                                Date = yearMonth,
                                ScriptDateTime = recordDateUtc,
                                ScriptCode = record.Script,
                                BestNet = netPerUnit,
                                BestACQ = record.AcquisitionCost / qty,
                                BestInsurancePayment = record.InsurancePayment / qty,
                                BestPatientPayment = record.PatientPayment / qty,
                                Qty = qty
                            };

                            newClassInsurances.Add(cii);
                            ciDict.Add(ciKey2, cii);
                        }
                    }
                }
            }

            // ========================================================
            // BULK SAVE DrugInsurances and ClassInsurances
            // ========================================================
            if (newDrugInsurances.Any())
            {
                _context.DrugInsurances.AddRange(newDrugInsurances);
            }

            if (newClassInsurances.Any())
            {
                _context.ClassInsurances.AddRange(newClassInsurances);
            }

            // Existing DrugInsurances/ClassInsurances are already tracked.
            // EF will save their modified values automatically.
            await _context.SaveChangesAsync(ct);

            // ========================================================
            // PHASE 3: Users, Scripts, DrugBranches
            // ========================================================
            var newUsers = new List<User>();
            var newScripts = new List<Script>();
            var newDrugBranches = new List<DrugBranch>();
            var random = new Random();

            foreach (var record in processedRecords)
            {
                if (!branchDict.TryGetValue(record.Branch, out var branch))
                    continue;

                if (!drugDict.TryGetValue(record.NDCCode, out var drug))
                    continue;

                // DrugBranch
                var tempKey = (branch.Id, drug.NDC);

                if (!drugBranchDict.ContainsKey(tempKey))
                {
                    var stock = random.Next(10, 101);

                    var db = new DrugBranch
                    {
                        BranchId = branch.Id,
                        DrugNDC = drug.NDC,
                        Stock = stock
                    };

                    newDrugBranches.Add(db);
                    drugBranchDict[tempKey] = db;
                }


                if (!string.IsNullOrWhiteSpace(record.Prescriber) && !userDict.ContainsKey(record.Prescriber))
                {
                    var safePrescriberName = record.Prescriber.Trim();

                    var p = new User
                    {
                        ShortName = safePrescriberName,
                        Name = safePrescriberName,
                        Email = safePrescriberName,
                        Password = BCrypt.Net.BCrypt.HashPassword("DefaultPass123"),
                        BranchId = branch.Id,
                        Role = Role.Doctor,
                    };

                    newUsers.Add(p);
                    userDict[safePrescriberName] = p;
                }
            }

            if (newUsers.Any())
            {
                _context.Users.AddRange(newUsers);
                await _context.SaveChangesAsync(ct);

                var shortNames = newUsers.Select(u => u.Email).ToList();

                var freshUsers = await _context.Users
                    .Where(u => shortNames.Contains(u.Email))
                    .ToListAsync(ct);

                foreach (var u in freshUsers)
                    userDict[u.Email] = u;
            }

            if (newDrugBranches.Any())
            {
                _context.DrugBranches.AddRange(newDrugBranches);
                await _context.SaveChangesAsync(ct);
            }

            // Scripts
            foreach (var record in processedRecords)
            {
                // ── CHANGED: multi-format date parsing ──
                if (!TryParseRecordDate(record.Date, out var rdLocal))
                    continue;

                var recordDateUtc = DateTime.SpecifyKind(rdLocal, DateTimeKind.Utc);

                if (!branchDict.TryGetValue(record.Branch, out var branch))
                    continue;

                var scriptKey = (record.Script, branch.Id);

                if (scriptDict.ContainsKey(scriptKey))
                    continue;

                userDict.TryGetValue(record.Prescriber ?? "", out var owner);

                var sc = new Script
                {
                    Date = recordDateUtc,
                    ScriptCode = record.Script,
                    BranchId = branch.Id,
                    UserId = owner?.Id
                };

                newScripts.Add(sc);
                scriptDict[scriptKey] = sc;
            }

            if (newScripts.Any())
            {
                _context.Scripts.AddRange(newScripts);
                await _context.SaveChangesAsync(ct);

                var scriptCodes = newScripts.Select(s => s.ScriptCode).Distinct().ToList();
                var branchIds = newScripts.Select(s => s.BranchId).Distinct().ToList();

                var freshScripts = await _context.Scripts
                    .Where(s => scriptCodes.Contains(s.ScriptCode) && branchIds.Contains(s.BranchId))
                    .ToListAsync(ct);

                foreach (var s in freshScripts)
                    scriptDict[(s.ScriptCode, s.BranchId)] = s;
            }

            // ========================================================
            // PHASE 4: ScriptItems - Bulk insert only
            // ========================================================
            var newScriptItems = new List<ScriptItem>();

            foreach (var record in processedRecords)
            {
                record.NDCCode = NormalizeNdcTo11Digits(record.NDCCode);

                // ── CHANGED: multi-format date parsing ──
                if (!TryParseRecordDate(record.Date, out var rdLocal))
                    continue;

                var recordDate = DateTime.SpecifyKind(rdLocal, DateTimeKind.Unspecified);
                var recordDateUtc = DateTime.SpecifyKind(recordDate, DateTimeKind.Utc);

                if (!insuranceRxDict.TryGetValue(record.RxGroup, out var insurance2))
                    continue;

                if (!drugDict.TryGetValue(record.NDCCode, out var drug2))
                    continue;

                if (!branchDict.TryGetValue(record.Branch, out var branchForScript))
                    continue;

                var scriptKey = (record.Script, branchForScript.Id);

                if (!scriptDict.TryGetValue(scriptKey, out var script))
                    continue;

                if (!userDict.TryGetValue(record.Prescriber, out var prescriber))
                    continue;

                var siKey = (script.Id, drug2.Id, record.RxNumber, recordDateUtc);

                if (scriptItemDic.ContainsKey(siKey))
                    continue;

                decimal realQTY = 1;

                if (!string.Equals(record.Quantity, "tableCell29", StringComparison.OrdinalIgnoreCase)
                    && decimal.TryParse(record.Quantity, NumberStyles.Any, CultureInfo.InvariantCulture, out var qx))
                {
                    realQTY = qx == 0 ? 1 : qx;
                }

                // ── Parse optional date fields (Day Supply End Date, Refill Date) ──
                DateTime? daySupplyEndDate = null;
                if (TryParseRecordDate(record.DaySupplyEndDate, out var dseTmp))
                {
                    daySupplyEndDate = DateTime.SpecifyKind(dseTmp, DateTimeKind.Utc);
                }

                DateTime? refillDate = null;
                if (TryParseRecordDate(record.RefillDate, out var rdTmp))
                {
                    refillDate = DateTime.SpecifyKind(rdTmp, DateTimeKind.Utc);
                }

                var newSI = new ScriptItem
                {
                    ScriptId = script.Id,
                    DrugId = drug2.Id,
                    InsuranceId = insurance2.Id,
                    RxNumber = record.RxNumber,
                    UserEmail = prescriber.Email,
                    PF = record.PF,
                    Quantity = realQTY,
                    RemainingStock = random.Next(10, 101),
                    AcquisitionCost = record.AcquisitionCost,
                    Discount = record.Discount,
                    InsurancePayment = record.InsurancePayment,
                    PatientPayment = record.PatientPayment,
                    NDCCode = record.NDCCode,

                    // ── NEW: NP verification fields ──
                    OriginalNetProfit = record.OriginalNetProfit,
                    GrossProfit = record.GrossProfit,

                    // ── NEW: Pricing references ──
                    AWP = record.AWP,
                    WAC = record.WAC,
                    SDRA = record.SDRA,

                    // ── NEW: Day Supply & Refill ──
                    Refill = record.Refill,
                    DaySupply = record.DaySupply,
                    DaySupplyEndDate = daySupplyEndDate,
                    RefillDate = refillDate,
                    Unit = string.IsNullOrWhiteSpace(record.Unit) ? null : record.Unit,

                    // ── NEW: Status fields ──
                    Status = string.IsNullOrWhiteSpace(record.Status) ? null : record.Status,
                    RxStatus = string.IsNullOrWhiteSpace(record.RxStatus) ? null : record.RxStatus,
                    Priority = string.IsNullOrWhiteSpace(record.Priority) ? null : record.Priority
                };

                newScriptItems.Add(newSI);
                scriptItemDic[siKey] = true;
            }

            if (newScriptItems.Any())
            {
                _context.ScriptItems.AddRange(newScriptItems);
                await _context.SaveChangesAsync(ct);
            }

            return newScriptItems.Count;
        }


        public static string NormalizeNdcTo11Digits(string ndcCode)
        {
            // Remove hyphens
            ndcCode = ndcCode.Replace("-", "");

            if (ndcCode.Length < 11)
            {
                ndcCode = ndcCode.PadLeft(11, '0');
            }

            // Return original if it matches the 11-digit format already
            return ndcCode;
        }

        public async Task<DrugInsurance> GetBySelection(string name, string ndc, string insuranceName)
        {
            var insurance = await _context.Insurances.FirstOrDefaultAsync(x => x.Name == insuranceName);
            var item = await _context.DrugInsurances.FirstOrDefaultAsync(x => x.NDCCode == ndc && x.InsuranceId == insurance.Id);
            return item;
        }



        internal async Task<Drug> SearchByIdNdc(int id, string ndc)
        {
            var item = await _context.Drugs.FirstOrDefaultAsync(x => x.Id == id && x.NDC == ndc);
            return item;
        }

        internal async Task<Drug> GetDrugByNdc(string ndc)
        {
            var item = await _context.Drugs.FirstOrDefaultAsync(x => x.NDC == ndc);
            return item;
        }
        internal async Task<(ICollection<string> InsuranceNDC, ICollection<string> AllInsuranceNDC)> GetDrugInsuranceNDCS(string drugName, int? insuranceId)
        {
            if (insuranceId != null)
            {
                // Console.WriteLine("Drug Name: " + drugName + " InsuranceId: " + insuranceId);
                // Console.ReadKey();
                var items = await _context.DrugInsurances.Include(x => x.Drug).Where(x => x.Drug.Name == drugName && x.InsuranceId == insuranceId && x.ScriptCode != null).Select(x => x.NDCCode).ToListAsync();
                // Console.ReadKey();
                var allItems = await _context.DrugInsurances.Include(x => x.Drug).Where(x => x.Drug.Name == drugName && x.InsuranceId != insuranceId && !items.Contains(x.NDCCode) && x.ScriptCode != null).Select(x => x.NDCCode).ToListAsync();

                // Console.WriteLine("Items Count: " + items.Count + " AllItems Count: " + allItems.Count);
                // Console.ReadKey();
                return (items, allItems);

            }
            var allItems2 = await _context.DrugInsurances.Include(x => x.Drug).Where(x => x.Drug.Name == drugName && x.ScriptCode != null).Select(x => x.NDCCode).ToListAsync();
            return (new List<string>(), allItems2);
        }
        internal async Task<DrugsAlternativesReadDto?> GetDetails(string ndc, int sourceInsuranceId, int? insuranceId = null,int branchId=1)
        {
            if (insuranceId == 0) insuranceId = null;

            // Base projection (newest first)
            IQueryable<dynamic> BaseQuery(bool requireScript, int? forceInsuranceId)
            {
                var q =
                    from di in _context.DrugInsurances.AsNoTracking()
                    join d in _context.Drugs.AsNoTracking() on di.DrugId equals d.Id
                    join irx in _context.InsuranceRxes.AsNoTracking() on di.InsuranceId equals irx.Id
                    join ipcn in _context.InsurancePCNs.AsNoTracking() on irx.InsurancePCNId equals ipcn.Id
                    join ins in _context.Insurances.AsNoTracking() on ipcn.InsuranceId equals ins.Id
                    where di.NDCCode == ndc && d.NDC == ndc
                    select new
                    {
                        DrugInsurance = di,
                        Bin = ins.Bin,
                        BinFullName = ins.Name,
                        RxGroup = irx.RxGroup,
                        PCN = ipcn.PCN,
                        pcnId = ipcn.Id,
                        rxgroupId = irx.Id,
                        binId = ins.Id,
                        Drug = d
                    };

                if (requireScript)
                    q = q.Where(x => x.DrugInsurance.ScriptCode != null && x.DrugInsurance.ScriptCode != "");

                if (forceInsuranceId.HasValue)
                    q = q.Where(x => x.DrugInsurance.InsuranceId == forceInsuranceId.Value);

                // newest per DI date/id
                return q.OrderByDescending(x => x.DrugInsurance.Date)
                        .ThenByDescending(x => x.DrugInsurance.Id);
            }

            // 1) Try with the provided insurance id (must have ScriptCode)
            var chosen = insuranceId.HasValue
                ? await BaseQuery(requireScript: true, forceInsuranceId: insuranceId).FirstOrDefaultAsync()
                : null;

            // 2) Fallback: any insurance for this NDC (still must have ScriptCode)
            if (chosen == null)
            {

                chosen = await BaseQuery(requireScript: true, forceInsuranceId: null).FirstOrDefaultAsync();
            }

            // 3) If still nothing with ScriptCode → return null (by design)
            if (chosen == null)
            {
                Console.WriteLine("No DrugInsurance found with ScriptCode for NDC: " + ndc + " and InsuranceId: " + insuranceId);
                return null;
            }


            // Effective InsuranceRxId from the selected row
            int effectiveInsuranceRxId = chosen.DrugInsurance.InsuranceId;

            // Fetch ALL statuses for this InsuranceRx + NDC (with reports)
            // Fetch ALL statuses for this InsuranceRx + NDC (with reports + users)
            var statuses = await _context.InsuranceStatuses
                .Where(s => s.InsuranceRxId == sourceInsuranceId && s.TargetDrugNDC == ndc)
                .Include(s => s.Reports)
                    .ThenInclude(r => r.User)
                .AsNoTracking()
                .ToListAsync();

            IEnumerable<Report> BranchReports(InsuranceStatus s) =>
                s.Reports?
                    .Where(r => r.User != null && r.User.BranchId == branchId)
                ?? Enumerable.Empty<Report>();

            DateTime LatestReportDate(InsuranceStatus s) =>
                BranchReports(s)
                    .OrderByDescending(r => r.StatusDate)
                    .Select(r => r.StatusDate)
                    .FirstOrDefault();

            var latestPAStatusRow = statuses
                .Where(s =>
                    !string.IsNullOrEmpty(s.PriorAuthorizationStatus) &&
                    s.PriorAuthorizationStatus != "NA" &&
                    BranchReports(s).Any())
                .Select(s => new
                {
                    Row = s,
                    Latest = LatestReportDate(s)
                })
                .OrderByDescending(x => x.Latest)
                .Select(x => x.Row)
                .FirstOrDefault();

            var latestApprovedStatusRow = statuses
                .Where(s =>
                    !string.IsNullOrEmpty(s.ApprovedStatus) &&
                    s.ApprovedStatus != "NA" &&
                    BranchReports(s).Any())
                .Select(s => new
                {
                    Row = s,
                    Latest = LatestReportDate(s)
                })
                .OrderByDescending(x => x.Latest)
                .Select(x => x.Row)
                .FirstOrDefault();

            var latestReport = statuses
                .SelectMany(s => BranchReports(s))
                .OrderByDescending(r => r.StatusDate)
                .FirstOrDefault();

            // Build DTO from the selected DI row (has ScriptCode)
            var dto = _mapper.Map<DrugsAlternativesReadDto>(chosen.DrugInsurance);

            dto.bin = chosen.Bin;
            dto.BinFullName = chosen.BinFullName;
            dto.rxgroup = chosen.RxGroup;
            dto.pcn = chosen.PCN;
            dto.pcnId = chosen.pcnId;
            dto.rxgroupId = chosen.rxgroupId;
            dto.binId = chosen.binId;

            dto.Quantity = chosen.DrugInsurance.Quantity == 0 ? 1 : chosen.DrugInsurance.Quantity;
            dto.DrugName = chosen.Drug.Name;

            dto.PriorAuthorizationStatus = latestPAStatusRow?.PriorAuthorizationStatus ?? "NA";
            dto.ApprovedStatus = latestApprovedStatusRow?.ApprovedStatus ?? "NA";
            dto.Status = latestReport?.Status ?? "Not Available";

            return dto;
        }
        internal async Task<DrugClass> getClassbyId(int id)
        {
            var item = await _context.DrugClasses.FirstOrDefaultAsync(x => x.Id == id);
            return item;
        }
        internal async Task<ClassInfo> getClassbyName(string name, string type = "ClassV1")
        {
            var item = await _context.ClassInfos.FirstOrDefaultAsync(x => x.Name == name);
            return item;
        }
        internal async Task<IEnumerable<ClassInfoReadDto>> GetClassesByDrugId(int drugId)
        {
            return await _context.DrugClasses
                                .Where(dc => dc.DrugId == drugId)
                                .Select(dc => new ClassInfoReadDto
                                {
                                    Id = dc.ClassInfo.Id,
                                    Name = dc.ClassInfo.Name,
                                    ClassTypeId = dc.ClassInfo.ClassTypeId,
                                    ClassTypeName = dc.ClassInfo.ClassType.Name
                                })
                                .ToListAsync();

        }
        internal async Task<ICollection<Drug>> GetDrugsByClass(int classId)
        {
            var items = await _context.Drugs
                .Where(x => x.DrugClasses.Any(dc => dc.ClassId == classId))
                .GroupBy(x => x.Name)
                .Select(g => g.First())
                .ToListAsync();
            return items;
        }
        internal async Task<ICollection<DrugInsurance>> GetAllLatest()
        {
            var items = await _context.DrugInsurances
                .AsNoTracking()
                .ToListAsync();
            return items;
        }

        private async Task<ICollection<DrugsAlternativesReadDto>> GetAllDrugsAlternativesDynamic(int classTypeId, string sourceDrugNDC, int pageNumber, int pageSize,bool isDemo = false)
        {
            if (isDemo)
            {
                pageNumber = DemoPageNumberLimit;
                pageSize = DemoPageSizeLimit;
            }
            // 1) Find the source drug
            var sourceDrug = await _context.Drugs
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.NDC == sourceDrugNDC);

            if (sourceDrug == null)
                return new List<DrugsAlternativesReadDto>();

            // 2) Find ALL classes (ClassInfoId) the source drug belongs to **for this class type**
            var sourceClassIds = await (
                from dc in _context.DrugClasses.AsNoTracking()
                join ci in _context.ClassInfos.AsNoTracking() on dc.ClassId equals ci.Id
                where dc.DrugId == sourceDrug.Id
                      && ci.ClassTypeId == classTypeId          // << filter by class type here
                select dc.ClassId
            ).Distinct().ToListAsync();

            if (sourceClassIds.Count == 0)
                return new List<DrugsAlternativesReadDto>();

            // 3) Query all alternative drugs that are in ANY of those class infos (same class type)
            var query =
                from dc in _context.DrugClasses
                where sourceClassIds.Contains(dc.ClassId)
                join d in _context.Drugs on dc.DrugId equals d.Id
                where d.NDC != sourceDrugNDC                       // exclude source drug
                join ci in _context.ClassInfos on dc.ClassId equals ci.Id
                where ci.ClassTypeId == classTypeId                // << and enforce class type again at read time

                // LEFT JOIN DrugInsurances
                join diGroup in _context.DrugInsurances on dc.DrugId equals diGroup.DrugId into diGroup
                from di in diGroup.DefaultIfEmpty()

                    // LEFT JOIN DrugBranches keyed by (DrugNDC, BranchId or default 1)
                join dbGroup in _context.DrugBranches
                    on new { DrugNDC = d.NDC, BranchId = di != null ? di.BranchId : 1 }
                    equals new { dbGroup.DrugNDC, dbGroup.BranchId } into dbGroup
                from db in dbGroup.DefaultIfEmpty()

                    // Latest report for (source, target, insurance)
                let latestReport =
                    di == null ? null :
                    _context.Reports
                        .Include(r => r.InsuranceStatus)
                        .Where(r =>
                            r.SourceDrugNDC == sourceDrugNDC &&
                            r.TargetDrugNDC == di.NDCCode &&
                            r.InsuranceRxId == di.InsuranceId)
                        .OrderByDescending(r => r.StatusDate)
                        .ThenByDescending(r => r.Id)
                        .FirstOrDefault()

                select new
                {
                    Drug = d,
                    DrugBranch = db,
                    DrugClass = dc,
                    ClassInfo = ci,
                    DrugInsurance = di,
                    LatestReport = latestReport
                };

            var list = await query.AsNoTracking().ToListAsync();

            // 4) Lookups
            var branchDict = await _context.Branches.AsNoTracking().ToDictionaryAsync(x => x.Id);

            var insuranceRxDict = await _context.InsuranceRxes
                .Include(ir => ir.InsurancePCN).ThenInclude(ipcn => ipcn.Insurance)
                .AsNoTracking()
                .ToDictionaryAsync(x => x.Id);

            // 5) Map to DTOs
            var result = list.Select(item =>
            {
                var di = item.DrugInsurance;
                var latest = item.LatestReport;

                var dto = _mapper.Map<DrugsAlternativesReadDto>(di ?? new DrugInsurance
                {
                    DrugId = item.Drug.Id,
                    NDCCode = item.Drug.NDC,
                    Net = 0,
                    Date = DateTime.UtcNow,
                    Quantity = 1,
                    AcquisitionCost = 0,
                    Discount = 0,
                    InsurancePayment = 0,
                    PatientPayment = 0,
                    BranchId = 1,
                    InsuranceId = 0,
                    Drug = item.Drug
                });

                dto.DrugName = item.Drug.Name;
                dto.NDCCode = item.Drug.NDC;
                dto.DrugClassId = item.DrugClass.Id;     // which DrugClass row this alternative came from
                dto.DrugClass = item.ClassInfo.Name;
                dto.Quantity = di?.Quantity ?? 1;
                dto.ApplicationNumber = item.Drug.ApplicationNumber;
                dto.ApplicationType = item.Drug.ApplicationType;
                dto.Route = item.Drug.Route;
                dto.Strength = item.Drug.Strength;
                dto.Form = item.Drug.Form;
                dto.Ingrdient = item.Drug.Ingrdient;
                dto.StrengthUnit = item.Drug.StrengthUnit;
                dto.Type = item.Drug.Type;
                dto.TECode = item.Drug.TECode;
                dto.Stock = item.DrugBranch?.Stock ?? 0;
                dto.ScriptCode = di?.ScriptCode;

                if (di != null && insuranceRxDict.TryGetValue(di.InsuranceId, out var insuranceRx))
                {
                    dto.insuranceName = insuranceRx.RxGroup;
                    dto.pcn = insuranceRx.InsurancePCN?.PCN;
                    dto.bin = insuranceRx.InsurancePCN?.Insurance?.Bin;
                    dto.rxgroup = insuranceRx.RxGroup;
                    dto.BinFullName = insuranceRx.InsurancePCN?.Insurance?.Name;
                    dto.binId = insuranceRx.InsurancePCN?.Insurance?.Id ?? 0;
                    dto.pcnId = insuranceRx.InsurancePCN?.Id ?? 0;
                    dto.rxgroupId = insuranceRx.Id;

                    dto.Status = latest?.Status ?? "Not Available";
                    dto.StatusDescription = latest?.StatusDescription ?? "No additional information";
                    dto.AdditionalInfo = latest?.AdditionalInfo;
                    dto.StatusDate = latest?.StatusDate;
                    dto.SubmitedUser = latest?.UserEmail ?? "";
                    dto.ApprovedStatus = latest?.InsuranceStatus?.ApprovedStatus ?? "NA";
                    dto.PriorAuthorizationStatus = latest?.InsuranceStatus?.PriorAuthorizationStatus ?? "NA";
                }

                if (di != null && branchDict.TryGetValue(di.BranchId, out var branch))
                    dto.branchName = branch.Name;

                if (dto.Quantity == 0)
                    dto.Quantity = 1;

                return dto;
            }).Skip((pageNumber - 1) * pageSize).Take(pageSize)
            // Optional: de-dupe if alternatives appear via multiple class infos
            // .GroupBy(x => new { x.NDCCode, x.rxgroupId })
            // .Select(g => g.First())
            .ToList();

            return result;
        }

        internal async Task<ICollection<DrugsAlternativesReadDto>> GetAllDrugs(int classInfoId, string sourceDrugNDC, int pageNumber, int pageSize,bool isDemo = false)
        {
            if (isDemo)
            {
                pageNumber = DemoPageNumberLimit;
                pageSize = DemoPageSizeLimit;
            }
            var tempCLass = await _context.ClassInfos.FirstOrDefaultAsync(x => x.Id == classInfoId);
            if (tempCLass.ClassTypeId >= 7)
            {
                return await GetAllDrugsAlternativesDynamic(tempCLass.ClassTypeId, sourceDrugNDC, pageNumber, pageSize);
            }
            var query =
                from dc in _context.DrugClasses
                where dc.ClassId == classInfoId
                join d in _context.Drugs on dc.DrugId equals d.Id
                join ci in _context.ClassInfos on dc.ClassId equals ci.Id
                join diGroup in _context.DrugInsurances on dc.DrugId equals diGroup.DrugId into diGroup
                from di in diGroup.DefaultIfEmpty()

                join dbGroup in _context.DrugBranches
                    on new { DrugNDC = d.NDC, BranchId = di != null ? di.BranchId : 1 }
                    equals new { dbGroup.DrugNDC, dbGroup.BranchId } into dbGroup
                from db in dbGroup.DefaultIfEmpty()

                    // NEW: correlated subquery to fetch the latest report for this (source, target, insurance)
                let latestReport =
                    di == null ? null :
                    _context.Reports
                    .Include(r => r.InsuranceStatus)
                        .Where(r =>
                            r.SourceDrugNDC == sourceDrugNDC &&
                            r.TargetDrugNDC == di.NDCCode &&
                            r.InsuranceRxId == di.InsuranceId)
                        .OrderByDescending(r => r.StatusDate)
                        .ThenByDescending(r => r.Id)          // tie-breaker
                        .FirstOrDefault()

                select new
                {
                    Drug = d,
                    DrugBranch = db,
                    DrugClass = dc,
                    ClassInfo = ci,
                    DrugInsurance = di,
                    LatestReport = latestReport             // << use this instead of InsuranceStatus
                };

            var list = await query.AsNoTracking().ToListAsync();

            var branchDict = await _context.Branches.AsNoTracking().ToDictionaryAsync(x => x.Id);
            var insuranceRxDict = await _context.InsuranceRxes
                .Include(ir => ir.InsurancePCN).ThenInclude(ipcn => ipcn.Insurance)
                .AsNoTracking()
                .ToDictionaryAsync(x => x.Id);

            var result = list.Select(item =>
            {
                var di = item.DrugInsurance;
                var latest = item.LatestReport;

                var dto = _mapper.Map<DrugsAlternativesReadDto>(di ?? new DrugInsurance
                {
                    DrugId = item.Drug.Id,
                    NDCCode = item.Drug.NDC,
                    Net = 0,
                    Date = DateTime.UtcNow,
                    Quantity = 1,
                    AcquisitionCost = 0,
                    Discount = 0,
                    InsurancePayment = 0,
                    PatientPayment = 0,
                    BranchId = 1,
                    InsuranceId = 0,
                    Drug = item.Drug
                });

                dto.DrugName = item.Drug.Name;
                dto.NDCCode = item.Drug.NDC;
                dto.DrugClassId = item.DrugClass.Id;
                dto.DrugClass = item.ClassInfo.Name;
                dto.Quantity = di?.Quantity ?? 1;
                dto.ApplicationNumber = item.Drug.ApplicationNumber;
                dto.ApplicationType = item.Drug.ApplicationType;
                dto.Route = item.Drug.Route;
                dto.Strength = item.Drug.Strength;
                dto.Form = item.Drug.Form;
                dto.Ingrdient = item.Drug.Ingrdient;
                dto.StrengthUnit = item.Drug.StrengthUnit;
                dto.Type = item.Drug.Type;
                dto.TECode = item.Drug.TECode;
                dto.Stock = item.DrugBranch?.Stock ?? 0;
                dto.ScriptCode = di?.ScriptCode;

                // Null checks for insuranceRxDict and di
                if (di != null && insuranceRxDict.TryGetValue(di.InsuranceId, out var insuranceRx))
                {
                    dto.insuranceName = insuranceRx.RxGroup;
                    dto.pcn = insuranceRx.InsurancePCN?.PCN;
                    dto.bin = insuranceRx.InsurancePCN?.Insurance?.Bin;
                    dto.rxgroup = insuranceRx.RxGroup;
                    dto.BinFullName = insuranceRx.InsurancePCN?.Insurance?.Name;
                    dto.binId = insuranceRx.InsurancePCN?.Insurance?.Id ?? 0;
                    dto.pcnId = insuranceRx.InsurancePCN?.Id ?? 0;
                    dto.rxgroupId = insuranceRx.Id;

                    // Null checks for latest and latest.InsuranceStatus
                    dto.Status = latest?.Status ?? "Not Available";
                    dto.StatusDescription = latest?.StatusDescription ?? "No additional information";
                    dto.AdditionalInfo = latest?.AdditionalInfo;
                    dto.StatusDate = latest?.StatusDate;
                    dto.SubmitedUser = latest?.UserEmail ?? "";
                    dto.ApprovedStatus = latest?.InsuranceStatus?.ApprovedStatus ?? "NA";
                    dto.PriorAuthorizationStatus = latest?.InsuranceStatus?.PriorAuthorizationStatus ?? "NA";
                }

                if (di != null && branchDict.TryGetValue(di.BranchId, out var branch))
                    dto.branchName = branch.Name;

                if (dto.Quantity == 0)
                    dto.Quantity = 1;

                return dto;
            }).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            return result;
        }

        // Returns ONLY alternatives that have insurance, filtered by rxgroup/pcn/bin,
        // sorted by Net DESC, paged (10 per page by default).


        // Put this inside your service class (private is fine)
        private sealed class BaseRow
        {
            public DrugClass Dc { get; init; } = default!;
            public Drug D { get; init; } = default!;
            public ClassInfo Ci { get; init; } = default!;
        }
        public sealed class PagedResult<T>
        {
            public required IReadOnlyList<T> Items { get; init; }
            public required int TotalCount { get; init; }
            public required int TotalPages { get; init; }
            public required int PageNumber { get; init; }
            public required int PageSize { get; init; }
        }

        public sealed class AlternativesFilterOptionsDto
        {
            public List<string> RxGroups { get; init; } = new();
            public List<string> Pcns { get; init; } = new();
            public List<string> Bins { get; init; } = new();
        }

        public sealed class DrugAlternativeLiteDto
        {
            public int DrugId { get; set; }
            public string DrugName { get; set; } = "";
            public string NDCCode { get; set; } = "";
            public int DrugClassId { get; set; }
            public string DrugClass { get; set; } = "";
            public string Form { get; set; } = "";
            public string Strength { get; set; } = "";
            public string StrengthUnit { get; set; } = "";
            public string Route { get; set; } = "";
            public string Type { get; set; } = "";
            public string TECode { get; set; } = "";
            public string ApplicationNumber { get; set; } = "";
            public string ApplicationType { get; set; } = "";
            public int Stock { get; set; }
            public string? BranchName { get; set; }
            public string DrugAlternativeStatus { get; set; } = "NA";
            public string? Status { get; internal set; }
            public string? StatusDescription { get; internal set; }
            public string? AdditionalInfo { get; internal set; }
            public DateTime? StatusDate { get; internal set; }
            public string ApprovedStatus { get; set; }
            public string PriorAuthorizationStatus { get; set; }
            public string SubmitedUser { get; internal set; }
        }

/*
        public async Task<PagedResult<DrugsAlternativesReadDto>> GetAlternativesWithInsurance(
            int classInfoId,
            string sourceDrugNDC,
            int sourceRxGroupId,
            int matchedRx,
            int pageNumber = 1,
            int pageSize = 10,
            string? rxgroup = null,
            string? pcn = null,
            string? bin = null,
            string? diseaseName = null)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var classInfo = await _context.ClassInfos.AsNoTracking()
                .FirstOrDefaultAsync(ci => ci.Id == classInfoId);
            if (classInfo == null) return EmptyPage(pageNumber, pageSize);

            var sourceDrug = await _context.Drugs.AsNoTracking()
                .FirstOrDefaultAsync(d => d.NDC == sourceDrugNDC);
            if (sourceDrug == null) return EmptyPage(pageNumber, pageSize);

            IQueryable<BaseRow> baseSet;
            if (classInfo.ClassTypeId >= 7)
            {
                var sourceClassIds = await (
                    from dc in _context.DrugClasses.AsNoTracking()
                    join ci in _context.ClassInfos.AsNoTracking() on dc.ClassId equals ci.Id
                    where dc.DrugId == sourceDrug.Id && ci.ClassTypeId == classInfo.ClassTypeId
                    select dc.ClassId
                ).Distinct().ToListAsync();

                if (sourceClassIds.Count == 0) return EmptyPage(pageNumber, pageSize);

                baseSet =
                    from dc in _context.DrugClasses
                    join d in _context.Drugs on dc.DrugId equals d.Id
                    join ci in _context.ClassInfos on dc.ClassId equals ci.Id
                    where sourceClassIds.Contains(dc.ClassId)
                          && ci.ClassTypeId == classInfo.ClassTypeId
                          && d.NDC != sourceDrugNDC
                    select new BaseRow { Dc = dc, D = d, Ci = ci };
            }
            else
            {
                baseSet =
                    from dc in _context.DrugClasses
                    join d in _context.Drugs on dc.DrugId equals d.Id
                    join ci in _context.ClassInfos on dc.ClassId equals ci.Id
                    where dc.ClassId == classInfoId && d.NDC != sourceDrugNDC
                    select new BaseRow { Dc = dc, D = d, Ci = ci };
            }

            // pre-filter DrugInsurances in SQL to only rows that have ScriptCode
            var diWithScript = _context.DrugInsurances.AsNoTracking()
                .Where(di => di.ScriptCode != null && di.ScriptCode != "");

            var query =
                from br in baseSet
                join di in diWithScript on br.Dc.DrugId equals di.DrugId
                join ir in _context.InsuranceRxes
                    .Include(x => x.InsurancePCN)
                    .ThenInclude(x => x.Insurance)
                    on di.InsuranceId equals ir.Id
                join dbGroup in _context.DrugBranches
                    on new { DrugNDC = br.D.NDC, BranchId = di.BranchId }
                    equals new { dbGroup.DrugNDC, dbGroup.BranchId } into dbGroup
                from db in dbGroup.DefaultIfEmpty()
                let latestDrugAlternativeReport =
                    _context.DrugAlternativeReports
                        .Where(r => r.SourceDrugNDC == sourceDrugNDC
                                    && r.TargetDrugNDC == di.NDCCode
                                    && r.ClassInfoId == br.Ci.Id)
                        .OrderByDescending(r => r.StatusDate)
                        .ThenByDescending(r => r.Id)
                        .FirstOrDefault()
                let latestInsuranceStatus =
                    _context.Reports.Include(r => r.InsuranceStatus)
                        .Where(r =>
                            r.TargetDrugNDC == di.NDCCode &&
                            r.InsuranceRxId == sourceRxGroupId)
                        .OrderByDescending(r => r.StatusDate)
                        .ThenByDescending(r => r.Id)
                        .FirstOrDefault()
                // newest per (NDC, InsuranceId)
                where di.Date == _context.DrugInsurances
                                    .Where(x => x.NDCCode == di.NDCCode && x.InsuranceId == di.InsuranceId)
                                    .Max(x => x.Date)
                select new
                {
                    Drug = br.D,
                    DrugClass = br.Dc,
                    ClassInfo = br.Ci,
                    DrugInsurance = di,
                    InsuranceRx = ir,
                    DrugBranch = db,
                    LatestDrugAlternativeReport = latestDrugAlternativeReport,
                    LatestInsuranceStatus = latestInsuranceStatus
                };

            // filters
            if (!string.IsNullOrWhiteSpace(rxgroup))
                query = query.Where(x => x.InsuranceRx.RxGroup == rxgroup);
            if (!string.IsNullOrWhiteSpace(pcn))
                query = query.Where(x => x.InsuranceRx.InsurancePCN != null && x.InsuranceRx.InsurancePCN.PCN == pcn);
            if (!string.IsNullOrWhiteSpace(bin))
                query = query.Where(x => x.InsuranceRx.InsurancePCN != null
                                      && x.InsuranceRx.InsurancePCN.Insurance != null
                                      && x.InsuranceRx.InsurancePCN.Insurance.Bin == bin);

            decimal PerItem(dynamic r)
                => r.DrugInsurance.Quantity > 0 ? r.DrugInsurance.Net / r.DrugInsurance.Quantity : r.DrugInsurance.Net;


    
    

// ========== END DISEASE FILTER ==========


            var projected = await query.ToListAsync(); // inputs already AsNoTracking
            var branchDict = await _context.Branches.AsNoTracking().ToDictionaryAsync(b => b.Id);


            List<DrugsAlternativesReadDto> grouped = new();

// Then continue with your grouping logic...

            if (!string.IsNullOrWhiteSpace(rxgroup))
            {
                // rxgroup filter → all rows (no per-NDC dedupe)
                grouped = projected
                    .OrderBy(r => r.Drug.NDC) // arrange by NDC
                    .ThenByDescending(r => r.DrugInsurance.Date)
                    .ThenByDescending(r => r.DrugInsurance.Id)
                    .Select(r => MapDto(r, branchDict))
                    .ToList();
            }
            else if (!string.IsNullOrWhiteSpace(bin))
            {
                // BIN ONLY → per NDC pick two (no dedupe), then arrange by NDC
                grouped = projected
                    .GroupBy(x => x.DrugInsurance.NDCCode)
                    .OrderBy(g => g.Key) // arrange groups by NDC
                    .SelectMany(g =>
                    {
                        var outRows = new List<DrugsAlternativesReadDto>();

                        // (1) most recent WITH RxGroup (fallback: most recent)
                        var mostRecentWithRx = g
                            .OrderByDescending(r => !string.IsNullOrWhiteSpace(r.InsuranceRx.RxGroup))
                            .ThenByDescending(r => r.DrugInsurance.Date)
                            .ThenByDescending(r => r.DrugInsurance.Id)
                            .FirstOrDefault();
                        if (mostRecentWithRx != null)
                            outRows.Add(MapDto(mostRecentWithRx, branchDict));

                        // (2) highest Net/Quantity
                        var bestPerItem = g
                            .OrderByDescending(r => PerItem(r))
                            .ThenByDescending(r => r.DrugInsurance.Date)
                            .ThenByDescending(r => r.DrugInsurance.Id)
                            .FirstOrDefault();
                        if (bestPerItem != null)
                            outRows.Add(MapDto(bestPerItem, branchDict));
                        // (3) matchedRx (latest if multiple). Only if matchedRx > 0 and exists in this group.
                        if (matchedRx > 0)
                        {
                            var matchedRxRow = g
                                .Where(r => r.DrugInsurance.InsuranceId == matchedRx) // or r.InsuranceRx.Id == matchedRx
                                .OrderByDescending(r => r.DrugInsurance.Date)
                                .ThenByDescending(r => r.DrugInsurance.Id)
                                .FirstOrDefault();
                            if (matchedRxRow != null) outRows.Add(MapDto(matchedRxRow, branchDict));
                        }

                        // dedupe by (NDC, ScriptCode) ONLY
                        var seen = new HashSet<string>();
                        var deduped = new List<DrugsAlternativesReadDto>();
                        foreach (var dto in outRows)
                        {
                            var key = $"{dto.NDCCode}|{dto.ScriptCode}";
                            if (seen.Add(key)) deduped.Add(dto);
                        }
                        return deduped;
                    })
                    .ToList();
            }
       

            else
            {
                // NO FILTERS → per NDC pick two (no dedupe), then arrange by NDC
                grouped = projected
                    .GroupBy(x => x.DrugInsurance.NDCCode)
                    .OrderBy(g => g.Key) // arrange groups by NDC
                    .SelectMany(g =>
                    {
                        var picks = new List<dynamic>();

                        // (1) most recent
                        var mostRecent = g
                            .OrderByDescending(r => r.DrugInsurance.Date)
                            .ThenByDescending(r => r.DrugInsurance.Id)
                            .FirstOrDefault();
                        if (mostRecent != null) picks.Add(mostRecent);

                        // (2) highest Net/Quantity
                        var bestPerItem = g
                            .OrderByDescending(r => PerItem(r))
                            .ThenByDescending(r => r.DrugInsurance.Date)
                            .ThenByDescending(r => r.DrugInsurance.Id)
                            .FirstOrDefault();
                        if (bestPerItem != null) picks.Add(bestPerItem);
                        if (matchedRx > 0)
                        {
                            var matchedRxRow = g
                                .Where(r => r.DrugInsurance.InsuranceId == matchedRx) // or r.InsuranceRx.Id == matchedRx
                                .OrderByDescending(r => r.DrugInsurance.Date)
                                .ThenByDescending(r => r.DrugInsurance.Id)
                                .FirstOrDefault();
                            if (matchedRxRow != null) picks.Add(matchedRxRow);
                        }

                        // dedupe by (NDC, ScriptCode) ONLY
                        var seen = new HashSet<string>();
                        var outRows = new List<DrugsAlternativesReadDto>();
                        foreach (var row in picks)
                        {
                            var dto = MapDto(row, branchDict);
                            var key = $"{dto.NDCCode}|{dto.ScriptCode}";
                            if (seen.Add(key)) outRows.Add(dto);
                        }
                        return outRows;
                    })
                    .ToList();
            }

            var totalCount = grouped.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            var skip = (pageNumber - 1) * pageSize;
            var items = grouped.Skip(skip).Take(pageSize).ToList();

            return new PagedResult<DrugsAlternativesReadDto>
            {
                Items = items,
                TotalCount = totalCount,
                TotalPages = totalPages,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
*/

public async Task<PagedResult<DrugsAlternativesReadDto>> GetAlternativesWithInsurance(
    int classInfoId,
    string sourceDrugNDC,
    int sourceRxGroupId,
    int matchedRx,
    int pageNumber = 1,
    int pageSize = 10,
    string? rxgroup = null,
    string? pcn = null,
    string? bin = null,
    string? diseaseName = null,int branchId = 1,bool isDemo = false)
{
    if (pageNumber < 1) pageNumber = 1;
    if (pageSize <= 0) pageSize = 10;
    if (pageSize > 100) pageSize = 100;
            if (isDemo)
            {
                pageNumber = DemoPageNumberLimit;
                pageSize = DemoPageSizeLimit;
            }
            var classInfo = await _context.ClassInfos.AsNoTracking()
        .FirstOrDefaultAsync(ci => ci.Id == classInfoId);
    if (classInfo == null) return EmptyPage(pageNumber, pageSize);

    var sourceDrug = await _context.Drugs.AsNoTracking()
        .FirstOrDefaultAsync(d => d.NDC == sourceDrugNDC);
    if (sourceDrug == null) return EmptyPage(pageNumber, pageSize);

    IQueryable<BaseRow> baseSet;
    if (classInfo.ClassTypeId >= 7)
    {
        var sourceClassIds = await (
            from dc in _context.DrugClasses.AsNoTracking()
            join ci in _context.ClassInfos.AsNoTracking() on dc.ClassId equals ci.Id
            where dc.DrugId == sourceDrug.Id && ci.ClassTypeId == classInfo.ClassTypeId
            select dc.ClassId
        ).Distinct().ToListAsync();

        if (sourceClassIds.Count == 0) return EmptyPage(pageNumber, pageSize);

        baseSet =
            from dc in _context.DrugClasses
            join d in _context.Drugs on dc.DrugId equals d.Id
            join ci in _context.ClassInfos on dc.ClassId equals ci.Id
            where sourceClassIds.Contains(dc.ClassId)
                  && ci.ClassTypeId == classInfo.ClassTypeId
                  && d.NDC != sourceDrugNDC
            select new BaseRow { Dc = dc, D = d, Ci = ci };
    }
    else
    {
        baseSet =
            from dc in _context.DrugClasses
            join d in _context.Drugs on dc.DrugId equals d.Id
            join ci in _context.ClassInfos on dc.ClassId equals ci.Id
            where dc.ClassId == classInfoId && d.NDC != sourceDrugNDC
            select new BaseRow { Dc = dc, D = d, Ci = ci };
    }

            // pre-filter DrugInsurances in SQL to only rows that have ScriptCode
            var branch = await _context.Branches.FirstOrDefaultAsync(x => x.Id == branchId);
    var diWithScript = _context.DrugInsurances.Include(x=>x.Branch).AsNoTracking()
        .Where(di => di.ScriptCode != null && di.ScriptCode != "" && di.Branch.MainCompanyId == branch.MainCompanyId);

    var query =
        from br in baseSet
        join di in diWithScript on br.Dc.DrugId equals di.DrugId
        join ir in _context.InsuranceRxes
            .Include(x => x.InsurancePCN)
            .ThenInclude(x => x.Insurance)
            on di.InsuranceId equals ir.Id
        join dbGroup in _context.DrugBranches
            on new { DrugNDC = br.D.NDC, BranchId = di.BranchId }
            equals new { dbGroup.DrugNDC, dbGroup.BranchId } into dbGroup
        from db in dbGroup.DefaultIfEmpty()
        let latestDrugAlternativeReport =
            _context.DrugAlternativeReports
                .Where(r => r.SourceDrugNDC == sourceDrugNDC
                            && r.TargetDrugNDC == di.NDCCode
                            && r.ClassInfoId == br.Ci.Id && r.User.BranchId==branchId)
                .OrderByDescending(r => r.StatusDate)
                .ThenByDescending(r => r.Id)
                .FirstOrDefault()
        let latestInsuranceStatus =
            _context.Reports.Include(r => r.InsuranceStatus)
                .Where(r =>
                    r.TargetDrugNDC == di.NDCCode &&
                    r.InsuranceRxId == sourceRxGroupId && r.User.BranchId==branchId)
                .OrderByDescending(r => r.StatusDate)
                .ThenByDescending(r => r.Id)
                .FirstOrDefault()
        // newest per (NDC, InsuranceId)
        where di.Date == _context.DrugInsurances
                            .Where(x => x.NDCCode == di.NDCCode && x.InsuranceId == di.InsuranceId)
                            .Max(x => x.Date)
        select new
        {
            Drug = br.D,
            DrugClass = br.Dc,
            ClassInfo = br.Ci,
            DrugInsurance = di,
            InsuranceRx = ir,
            DrugBranch = db,
            LatestDrugAlternativeReport = latestDrugAlternativeReport,
            LatestInsuranceStatus = latestInsuranceStatus
        };

    // filters
    if (!string.IsNullOrWhiteSpace(rxgroup))
        query = query.Where(x => x.InsuranceRx.RxGroup == rxgroup);

    if (!string.IsNullOrWhiteSpace(pcn))
        query = query.Where(x => x.InsuranceRx.InsurancePCN != null && x.InsuranceRx.InsurancePCN.PCN == pcn);

    if (!string.IsNullOrWhiteSpace(bin))
        query = query.Where(x => x.InsuranceRx.InsurancePCN != null
                              && x.InsuranceRx.InsurancePCN.Insurance != null
                              && x.InsuranceRx.InsurancePCN.Insurance.Bin == bin);


if (!string.IsNullOrWhiteSpace(diseaseName))
{
    var dn = diseaseName.Trim().ToLower();

    var diseaseDrugIds = await _context.DrugDiseaseAddHistories
        .AsNoTracking()
        .Where(dd =>
            dd.Show == true &&
            dd.Disease != null &&
            dd.Disease.Name != null &&
            dd.Disease.Name.ToLower() == dn
        )
        .Select(dd => dd.DrugId)
        .Distinct()
        .ToListAsync();

    if (diseaseDrugIds.Count == 0)
        return EmptyPage(pageNumber, pageSize);

    query = query.Where(x => diseaseDrugIds.Contains(x.Drug.Id));
}

    // ========== END DISEASE FILTER ==========

    decimal PerItem(dynamic r)
        => r.DrugInsurance.Quantity > 0 ? r.DrugInsurance.Net / r.DrugInsurance.Quantity : r.DrugInsurance.Net;

    var projected = await query.ToListAsync(); // inputs already AsNoTracking
    var branchDict = await _context.Branches.AsNoTracking().ToDictionaryAsync(b => b.Id);

    List<DrugsAlternativesReadDto> grouped = new();

    if (!string.IsNullOrWhiteSpace(rxgroup))
    {
        // rxgroup filter → all rows (no per-NDC dedupe)
        grouped = projected
            .OrderBy(r => r.Drug.NDC) // arrange by NDC
            .ThenByDescending(r => r.DrugInsurance.Date)
            .ThenByDescending(r => r.DrugInsurance.Id)
            .Select(r => MapDto(r, branchDict))
            .ToList();
    }
    else if (!string.IsNullOrWhiteSpace(bin))
    {
        // BIN ONLY → per NDC pick two (no dedupe), then arrange by NDC
        grouped = projected
            .GroupBy(x => x.DrugInsurance.NDCCode)
            .OrderBy(g => g.Key) // arrange groups by NDC
            .SelectMany(g =>
            {
                var outRows = new List<DrugsAlternativesReadDto>();

                // (1) most recent WITH RxGroup (fallback: most recent)
                var mostRecentWithRx = g
                    .OrderByDescending(r => !string.IsNullOrWhiteSpace(r.InsuranceRx.RxGroup))
                    .ThenByDescending(r => r.DrugInsurance.Date)
                    .ThenByDescending(r => r.DrugInsurance.Id)
                    .FirstOrDefault();
                if (mostRecentWithRx != null)
                    outRows.Add(MapDto(mostRecentWithRx, branchDict));

                // (2) highest Net/Quantity
                var bestPerItem = g
                    .OrderByDescending(r => PerItem(r))
                    .ThenByDescending(r => r.DrugInsurance.Date)
                    .ThenByDescending(r => r.DrugInsurance.Id)
                    .FirstOrDefault();
                if (bestPerItem != null)
                    outRows.Add(MapDto(bestPerItem, branchDict));

                // (3) matchedRx (latest if multiple). Only if matchedRx > 0 and exists in this group.
                if (matchedRx > 0)
                {
                    var matchedRxRow = g
                        .Where(r => r.DrugInsurance.InsuranceId == matchedRx) // or r.InsuranceRx.Id == matchedRx
                        .OrderByDescending(r => r.DrugInsurance.Date)
                        .ThenByDescending(r => r.DrugInsurance.Id)
                        .FirstOrDefault();
                    if (matchedRxRow != null) outRows.Add(MapDto(matchedRxRow, branchDict));
                }

                // dedupe by (NDC, ScriptCode) ONLY
                var seen = new HashSet<string>();
                var deduped = new List<DrugsAlternativesReadDto>();
                foreach (var dto in outRows)
                {
                    var key = $"{dto.NDCCode}|{dto.ScriptCode}";
                    if (seen.Add(key)) deduped.Add(dto);
                }
                return deduped;
            })
            .ToList();
    }
    else
    {
        // NO FILTERS → per NDC pick two (no dedupe), then arrange by NDC
        grouped = projected
            .GroupBy(x => x.DrugInsurance.NDCCode)
            .OrderBy(g => g.Key) // arrange groups by NDC
            .SelectMany(g =>
            {
                var picks = new List<dynamic>();

                // (1) most recent
                var mostRecent = g
                    .OrderByDescending(r => r.DrugInsurance.Date)
                    .ThenByDescending(r => r.DrugInsurance.Id)
                    .FirstOrDefault();
                if (mostRecent != null) picks.Add(mostRecent);

                // (2) highest Net/Quantity
                var bestPerItem = g
                    .OrderByDescending(r => PerItem(r))
                    .ThenByDescending(r => r.DrugInsurance.Date)
                    .ThenByDescending(r => r.DrugInsurance.Id)
                    .FirstOrDefault();
                if (bestPerItem != null) picks.Add(bestPerItem);

                if (matchedRx > 0)
                {
                    var matchedRxRow = g
                        .Where(r => r.DrugInsurance.InsuranceId == matchedRx) // or r.InsuranceRx.Id == matchedRx
                        .OrderByDescending(r => r.DrugInsurance.Date)
                        .ThenByDescending(r => r.DrugInsurance.Id)
                        .FirstOrDefault();
                    if (matchedRxRow != null) picks.Add(matchedRxRow);
                }

                // dedupe by (NDC, ScriptCode) ONLY
                var seen = new HashSet<string>();
                var outRows = new List<DrugsAlternativesReadDto>();
                foreach (var row in picks)
                {
                    var dto = MapDto(row, branchDict);
                    var key = $"{dto.NDCCode}|{dto.ScriptCode}";
                    if (seen.Add(key)) outRows.Add(dto);
                }
                return outRows;
            })
            .ToList();
    }

    var totalCount = isDemo==false ?grouped.Count :DemoPageSizeLimit ;
    var totalPages = isDemo==false?(int)Math.Ceiling(totalCount / (double)pageSize): DemoPageNumberLimit;
    var skip = (pageNumber - 1) * pageSize;
    var items = grouped.Skip(skip).Take(pageSize).ToList();

    return new PagedResult<DrugsAlternativesReadDto>
    {
        Items = items,
        TotalCount = totalCount,
        TotalPages = totalPages,
        PageNumber = pageNumber,
        PageSize = pageSize
    };
}

// --- helpers ---

private static PagedResult<DrugsAlternativesReadDto> EmptyPage(int pageNumber, int pageSize) => new()
{
    Items = Array.Empty<DrugsAlternativesReadDto>(),
    TotalCount = 0,
    TotalPages = 0,
    PageNumber = pageNumber,
    PageSize = pageSize
};

        // --- helpers ---

        private static PagedResult<DrugsAlternativesReadDto> EmptyPage1(int pageNumber, int pageSize) => new()
        {
            Items = Array.Empty<DrugsAlternativesReadDto>(),
            TotalCount = 0,
            TotalPages = 0,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        private DrugsAlternativesReadDto MapDto(dynamic best, Dictionary<int, Branch> branchDict)
        {
            var di = best.DrugInsurance;
            var ir = best.InsuranceRx;
            var dto = _mapper.Map<DrugsAlternativesReadDto>(di);

            dto.DrugName = best.Drug.Name;
            dto.NDCCode = best.Drug.NDC;
            dto.DrugClassId = best.DrugClass.Id;
            dto.DrugClass = best.ClassInfo.Name;
            dto.Quantity = di.Quantity > 0 ? di.Quantity : 1;
            dto.ApplicationNumber = best.Drug.ApplicationNumber;
            dto.ApplicationType = best.Drug.ApplicationType;
            dto.Route = best.Drug.Route;
            dto.Strength = best.Drug.Strength;
            dto.Form = best.Drug.Form;
            dto.Ingrdient = best.Drug.Ingrdient;
            dto.StrengthUnit = best.Drug.StrengthUnit;
            dto.Type = best.Drug.Type;
            dto.TECode = best.Drug.TECode;
            dto.Stock = best.DrugBranch?.Stock ?? 0;
            dto.ScriptCode = di.ScriptCode;

            dto.insuranceName = ir.RxGroup;
            dto.pcn = ir.InsurancePCN?.PCN;
            dto.bin = ir.InsurancePCN?.Insurance?.Bin;
            dto.rxgroup = ir.RxGroup;
            dto.BinFullName = ir.InsurancePCN?.Insurance?.Name;
            dto.binId = ir.InsurancePCN?.Insurance?.Id ?? 0;
            dto.pcnId = ir.InsurancePCN?.Id ?? 0;
            dto.rxgroupId = ir.Id;

            dto.Status = best.LatestInsuranceStatus?.Status ?? "Not Available";
            dto.PriorAuthorizationStatus = best.LatestInsuranceStatus?.InsuranceStatus?.PriorAuthorizationStatus ?? "NA";
            dto.ApprovedStatus = best.LatestInsuranceStatus?.InsuranceStatus?.ApprovedStatus ?? "NA";
            dto.DrugAlternativeStatus = best.LatestDrugAlternativeReport?.Status ?? "NA";

            if (branchDict.TryGetValue(di.BranchId, out Branch branch))
                dto.branchName = branch.Name;

            if (dto.Quantity == 0) dto.Quantity = 1;
            return dto;
        }


        public async Task<AlternativesFilterOptionsDto> GetAlternativesWithInsuranceFilters(
            int classInfoId,
            string sourceDrugNDC,
            string? rxgroup = null,
            string? pcn = null,
            string? bin = null)
        {
            var classInfo = await _context.ClassInfos.AsNoTracking().FirstOrDefaultAsync(ci => ci.Id == classInfoId);
            if (classInfo == null) return new AlternativesFilterOptionsDto();

            var sourceDrug = await _context.Drugs.AsNoTracking().FirstOrDefaultAsync(d => d.NDC == sourceDrugNDC);
            if (sourceDrug == null) return new AlternativesFilterOptionsDto();

            IQueryable<BaseRow> baseSet;

            if (classInfo.ClassTypeId >= 7)
            {
                var sourceClassIds = await (
                    from dc in _context.DrugClasses.AsNoTracking()
                    join ci in _context.ClassInfos.AsNoTracking() on dc.ClassId equals ci.Id
                    where dc.DrugId == sourceDrug.Id && ci.ClassTypeId == classInfo.ClassTypeId
                    select dc.ClassId
                ).Distinct().ToListAsync();

                if (sourceClassIds.Count == 0) return new AlternativesFilterOptionsDto();

                baseSet =
                    from dc in _context.DrugClasses
                    join d in _context.Drugs on dc.DrugId equals d.Id
                    join ci in _context.ClassInfos on dc.ClassId equals ci.Id
                    where sourceClassIds.Contains(dc.ClassId)
                          && ci.ClassTypeId == classInfo.ClassTypeId
                          && d.NDC != sourceDrugNDC
                    select new BaseRow { Dc = dc, D = d, Ci = ci };
            }
            else
            {
                baseSet =
                    from dc in _context.DrugClasses
                    join d in _context.Drugs on dc.DrugId equals d.Id
                    join ci in _context.ClassInfos on dc.ClassId equals ci.Id
                    where dc.ClassId == classInfoId && d.NDC != sourceDrugNDC
                    select new BaseRow { Dc = dc, D = d, Ci = ci };
            }

            // Base relation that only includes rows WITH insurance
            var rel =
                from br in baseSet
                join di in _context.DrugInsurances on br.Dc.DrugId equals di.DrugId
                where di.ScriptCode != null
                join ir in _context.InsuranceRxes on di.InsuranceId equals ir.Id
                select new { ir };

            // Cascading lists:
            // RxGroups depend on PCN+BIN filters
            var rxQuery = rel.AsQueryable();
            if (!string.IsNullOrWhiteSpace(pcn))
                rxQuery = rxQuery.Where(x => x.ir.InsurancePCN != null && x.ir.InsurancePCN.PCN == pcn);
            if (!string.IsNullOrWhiteSpace(bin))
                rxQuery = rxQuery.Where(x => x.ir.InsurancePCN != null
                                          && x.ir.InsurancePCN.Insurance != null
                                          && x.ir.InsurancePCN.Insurance.Bin == bin);

            var rxgroups = await rxQuery
                .Select(x => x.ir.RxGroup)
                .Where(v => v != null && v != "")
                .Distinct()
                .OrderBy(v => v)
                .ToListAsync();

            // PCNs depend on RxGroup+BIN filters
            var pcnQuery = rel.AsQueryable();
            if (!string.IsNullOrWhiteSpace(rxgroup))
                pcnQuery = pcnQuery.Where(x => x.ir.RxGroup == rxgroup);
            if (!string.IsNullOrWhiteSpace(bin))
                pcnQuery = pcnQuery.Where(x => x.ir.InsurancePCN != null
                                            && x.ir.InsurancePCN.Insurance != null
                                            && x.ir.InsurancePCN.Insurance.Bin == bin);

            var pcns = await pcnQuery
                .Select(x => x.ir.InsurancePCN!.PCN!)
                .Where(v => v != null && v != "")
                .Distinct()
                .OrderBy(v => v)
                .ToListAsync();

            // BINs depend on RxGroup+PCN filters
            var binQuery = rel.AsQueryable();
            if (!string.IsNullOrWhiteSpace(rxgroup))
                binQuery = binQuery.Where(x => x.ir.RxGroup == rxgroup);
            if (!string.IsNullOrWhiteSpace(pcn))
                binQuery = binQuery.Where(x => x.ir.InsurancePCN != null && x.ir.InsurancePCN.PCN == pcn);

            var bins = await binQuery
                .Select(x => x.ir.InsurancePCN!.Insurance!.Bin!)
                .Where(v => v != null && v != "")
                .Distinct()
                .OrderBy(v => v)
                .ToListAsync();

            return new AlternativesFilterOptionsDto
            {
                RxGroups = rxgroups,
                Pcns = pcns,
                Bins = bins
            };
        }


        public async Task<PagedResult<DrugAlternativeLiteDto>> GetAlternativesNoInsurancePaged(
    int classInfoId,
    int rxgroupId,
    string sourceDrugNDC,
    int pageNumber = 1,
    int pageSize = 10, int branchId = 0,bool isDemo = false)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;
            if (isDemo)
            {
                pageNumber = DemoPageNumberLimit;
                pageSize = DemoPageSizeLimit;
            }
            var classInfo = await _context.ClassInfos.AsNoTracking()
                .FirstOrDefaultAsync(ci => ci.Id == classInfoId);

            if (classInfo == null)
            {
                return new PagedResult<DrugAlternativeLiteDto>
                {
                    Items = Array.Empty<DrugAlternativeLiteDto>(),
                    TotalCount = 0,
                    TotalPages = 0,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
            }

            var sourceDrug = await _context.Drugs.AsNoTracking()
                .FirstOrDefaultAsync(d => d.NDC == sourceDrugNDC);

            if (sourceDrug == null)
            {
                return new PagedResult<DrugAlternativeLiteDto>
                {
                    Items = Array.Empty<DrugAlternativeLiteDto>(),
                    TotalCount = 0,
                    TotalPages = 0,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
            }

            IQueryable<BaseRow> baseSet;
            var branch = await  _context.Branches.FirstOrDefaultAsync(x => x.Id == branchId);
            if (classInfo.ClassTypeId >= 7)
            {
                var sourceClassIds = await (
                    from dc in _context.DrugClasses.AsNoTracking()
                    join ci in _context.ClassInfos.AsNoTracking() on dc.ClassId equals ci.Id
                    where dc.DrugId == sourceDrug.Id && ci.ClassTypeId == classInfo.ClassTypeId
                    select dc.ClassId
                ).Distinct().ToListAsync();

                if (sourceClassIds.Count == 0)
                {
                    return new PagedResult<DrugAlternativeLiteDto>
                    {
                        Items = Array.Empty<DrugAlternativeLiteDto>(),
                        TotalCount = 0,
                        TotalPages = 0,
                        PageNumber = pageNumber,
                        PageSize = pageSize
                    };
                }

                baseSet =
                    from dc in _context.DrugClasses
                    join d in _context.Drugs on dc.DrugId equals d.Id
                    join ci in _context.ClassInfos on dc.ClassId equals ci.Id
                    join di in _context.DrugInsurances
                            .Where(x => x.Branch.MainCompanyId == branch.MainCompanyId) on dc.DrugId equals di.DrugId into diGroup
                    from di in diGroup.DefaultIfEmpty()
                    where sourceClassIds.Contains(dc.ClassId)
                          && ci.ClassTypeId == classInfo.ClassTypeId
                          && d.NDC != sourceDrugNDC
                          && di == null                 // ✅ only drugs with no insurance
                    select new BaseRow { Dc = dc, D = d, Ci = ci };
            }
            else
            {
                baseSet =
                        from dc in _context.DrugClasses
                        join d in _context.Drugs on dc.DrugId equals d.Id
                        join ci in _context.ClassInfos on dc.ClassId equals ci.Id
                        join di in _context.DrugInsurances
                            .Where(x => x.Branch.MainCompanyId == branch.MainCompanyId)
                            on dc.DrugId equals di.DrugId into diGroup
                        from di in diGroup.DefaultIfEmpty()
                        where dc.ClassId == classInfoId
                              && d.NDC != sourceDrugNDC
                              && di == null
                        select new BaseRow
                        {
                            Dc = dc,
                            D = d,
                            Ci = ci
                        };
            }

            // Optional branch info (default branch = 1)
            var flat = await (
                from br in baseSet
                join dbGroup in _context.DrugBranches
                    on new { DrugNDC = br.D.NDC, BranchId = 1 }
                    equals new { dbGroup.DrugNDC, dbGroup.BranchId } into dbGroup
                from db in dbGroup.DefaultIfEmpty()
                let latestDrugAlternativeReport =
                    _context.DrugAlternativeReports
                        .Where(r => r.SourceDrugNDC == sourceDrugNDC
                                    && r.TargetDrugNDC == br.D.NDC
                                    && r.ClassInfoId == br.Ci.Id && r.User.BranchId==branchId)  
                        .OrderByDescending(r => r.StatusDate)
                        .ThenByDescending(r => r.Id)
                        .FirstOrDefault()
                let latestInsuranceStatus =
                    _context.Reports.Include(r => r.InsuranceStatus)
                        .Where(r =>
                            r.TargetDrugNDC == br.D.NDC &&
                            r.InsuranceRxId == rxgroupId && r.User.BranchId == branchId)
                        .OrderByDescending(r => r.StatusDate)
                        .ThenByDescending(r => r.Id)
                        .FirstOrDefault()
                select new
                {
                    br.D,
                    br.Dc,
                    br.Ci,
                    DrugBranch = db,
                    LatestDrugAlternativeReport = latestDrugAlternativeReport,
                    LatestInsuranceStatus = latestInsuranceStatus
                }
            ).AsNoTracking().ToListAsync();

            // Deduplicate by NDC
            var dedup = flat
                .GroupBy(x => x.D.NDC)
                .Select(g =>
                {
                    var best = g.OrderByDescending(r => r.DrugBranch != null ? r.DrugBranch.Stock : 0)
                                .ThenBy(r => r.D.Name)
                                .First();

                    var latestReport = _context.DrugAlternativeReports
                        .Where(r => r.SourceDrugNDC == sourceDrugNDC
                                    && r.TargetDrugNDC == best.D.NDC
                                    && r.ClassInfoId == best.Ci.Id)
                        .OrderByDescending(r => r.StatusDate)
                        .ThenByDescending(r => r.Id)
                        .FirstOrDefault();

                    return new DrugAlternativeLiteDto
                    {
                        DrugId = best.D.Id,
                        DrugName = best.D.Name,
                        NDCCode = best.D.NDC,
                        DrugClassId = best.Dc.Id,
                        DrugClass = best.Ci.Name,
                        Form = best.D.Form,
                        Strength = best.D.Strength,
                        StrengthUnit = best.D.StrengthUnit,
                        Route = best.D.Route,
                        Type = best.D.Type,
                        TECode = best.D.TECode,
                        ApplicationNumber = best.D.ApplicationNumber,
                        ApplicationType = best.D.ApplicationType,
                        Stock = best.DrugBranch?.Stock ?? 0,
                        BranchName = "",
                        DrugAlternativeStatus = latestReport?.Status ?? "NA",
                        Status = best.LatestInsuranceStatus?.Status ?? "Not Available",
                        StatusDescription = best.LatestInsuranceStatus?.StatusDescription,
                        AdditionalInfo = best.LatestInsuranceStatus?.AdditionalInfo,
                        StatusDate = best.LatestInsuranceStatus?.StatusDate,
                        ApprovedStatus = best.LatestInsuranceStatus?.InsuranceStatus?.ApprovedStatus ?? "NA",
                        PriorAuthorizationStatus = best.LatestInsuranceStatus?.InsuranceStatus?.PriorAuthorizationStatus ?? "NA",
                        SubmitedUser = best.LatestInsuranceStatus?.User?.Name
                    };
                })
                .OrderBy(x => x.DrugName)
                .ThenByDescending(x => x.Stock)
                .ToList();

            var totalCount = isDemo == false ? dedup.Count : DemoPageSizeLimit;
            var totalPages = isDemo==false ?  (int)Math.Ceiling(totalCount / (double)pageSize) : DemoPageNumberLimit;
            var skip = (pageNumber - 1) * pageSize;

            return new PagedResult<DrugAlternativeLiteDto>
            {
                Items = dedup.Skip(skip).Take(pageSize).ToList(),
                TotalCount = totalCount,
                TotalPages = totalPages,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        internal async Task<Drug> GetDrugById(int id)
        {
            var item = await _context.Drugs.FirstOrDefaultAsync(x => x.Id == id);
            return item;
        }

        // internal async Task oneway()
        // {
        //     var drugInsurances = await _context.DrugInsurances.ToListAsync();
        //     var insurances = await _context.Insurances.ToDictionaryAsync(i => i.Id, i => i.Name);

        //     foreach (var drugInsurance in drugInsurances)
        //     {
        //         if (insurances.TryGetValue(drugInsurance.InsuranceId, out var insuranceName))
        //         {
        //             drugInsurance.insuranceName = insuranceName;
        //         }
        //     }

        //     await _context.SaveChangesAsync();
        // }

        public class DrugInsuranceRecord
        {
            [Name("Ins")]
            public string InsuranceName { get; set; }

            [Name("Drug Name")]
            public string DrugName { get; set; }

            [Name("NDC")]
            public string NDCCode { get; set; }
        }
        public class ScriptRecord
        {
            // ── Date & Identification ──
            [Name("Date Filled")]
            public string Date { get; set; }

            [Name("Script")]
            public string Script { get; set; }

            [Name("Rx Number")]
            public string RxNumber { get; set; }

            [Name("Refill")]
            public int? Refill { get; set; }

            // ── Insurance Routing ──
            [Name("Insurance")]
            public string Insurance { get; set; }

            [Name("BIN")]
            public string Bin { get; set; }

            [Name("PCN")]
            public string PCN { get; set; }

            [Name("Group")]
            public string RxGroup { get; set; }

            // ── User & Prescriber ──
            [Name("User")]
            public string? User { get; set; }

            [Name("Prescriber")]
            public string Prescriber { get; set; }

            [Name("Office")]
            public string Branch { get; set; }

            // ── Drug Identification ──
            [Name("Dispensed Item")]
            public string DrugName { get; set; }

            [Name("NDC")]
            public string NDCCode { get; set; }

            [Name("Unit")]
            public string? Unit { get; set; }

            // ── Quantity & Day Supply ──
            [Name("Quantity")]
            public string Quantity { get; set; }

            [Name("Day Supply")]
            public int? DaySupply { get; set; }

            [Name("Day Supply \nEnd Date")]
            public string? DaySupplyEndDate { get; set; }

            [Name("Refill Date")]
            public string? RefillDate { get; set; }

            // ── Pricing ──
            [Name("AWP")]
            public decimal? AWP { get; set; }

            [Name("WAC")]
            public decimal? WAC { get; set; }

            [Name("ACQ")]
            public decimal AcquisitionCost { get; set; }

            [Name("Copay")]
            public decimal PatientPayment { get; set; }

            [Name("SDRA")]
            public decimal? SDRA { get; set; }

            // ── Net Profit (source system) ──
            [Name("NP")]
            public decimal? OriginalNetProfit { get; set; }

            [Name("GP")]
            public decimal? GrossProfit { get; set; }

            // ── Status Fields ──
            [Name("Status")]
            public string? Status { get; set; }

            [Name("Rx Status")]
            public string? RxStatus { get; set; }

            [Name("Priority")]
            public string? Priority { get; set; }

            // ── Discount kept for backwards compatibility ──
            public decimal Discount { get; set; } = 0;

            // ── PF kept for backwards compatibility ──
            public string PF { get; set; } = "INS";

            // ── Derived field — Insurance Payment ──
            // The client's CSV does not have an explicit "Ins Pay" column.
            // It is reverse-calculated from NP + ACQ - Copay.
            // SDRA, when populated, IS the insurance payment directly.
            public decimal InsurancePayment
            {
                get
                {
                    if (SDRA.HasValue && SDRA.Value > 0)
                        return SDRA.Value;

                    if (OriginalNetProfit.HasValue)
                        return OriginalNetProfit.Value + AcquisitionCost - PatientPayment;

                    return 0;
                }
            }

            public int RemainingStock { get; set; } = 0;
        }

        public class DrugCs
        {
            [Name("drug_name")]
            public string? Name { get; set; }

            [Name("ndc")]
            public string? NDC { get; set; }

            [Name("form")]
            public string? Form { get; set; }

            [Name("strength")]
            public string? Strength { get; set; }

            [Name("acq")]
            public decimal? ACQ { get; set; }

            [Name("awp")]
            public decimal? AWP { get; set; }

            [Name("rxCUI")]
            [Default(0)]
            public decimal? Rxcui { get; set; }

            [Name("drug_class")]
            public string? DrugClass { get; set; }
            [Name("route")]
            public string? Route { get; set; }
            [Name("TE_Code")]
            public string? TECode { get; set; }
            [Name("ingredient")]
            public string? Ingrdient { get; set; }
            [Name("Appl_No")]
            public string? ApplicationNumber { get; set; }
            [Name("Appl_Type")]
            public string? ApplicationType { get; set; }
            [Name("Adjusted_Group_ID")]
            public string? ClassV2 { get; set; }
            [Name("Unit")]
            public string? Unit { get; set; }
            [Name("Type")]
            public string? Type { get; set; }
            [Name("Group_By_Class_Standardized")]
            public string? ClassV3 { get; set; }
            [Name("EPC_Class_Name_Cleaned")]
            public string? ClassV4 { get; set; }
            [Name("ROUTE_AND_CLASS_RENAMED_FOR_PHARMACIST")]
            public string? ClassV5 { get; set; }
            [Name("PHARM_CLASSES")]
            public string? PHARM_CLASSES { get; set; }
        }

        public async Task<ICollection<AuditReadDto>> GetAllLatestScriptsPaginated(int pageNumber, int pageSize, string classVersion = "ClassV6", string matchOn = "BIN",int mainCompanyId=1,int branchId=0)
        {
            // Use classVersion as part of the cache key
            string cacheKey = $"AllLatestScripts_{classVersion}_{matchOn}_{branchId}";
            List<AuditReadDto> allData;

            // Try to get the specific classVersion from the cache
            if (!_cache.TryGetValue(cacheKey, out allData) && branchId!=0)
            {
                Console.WriteLine("Hii the cachKey : " + cacheKey);

                // Cache miss: Load the entire dataset for this classVersion
                allData = await GetAuditDtosWithBestBeforeOrPrevMonthAsync(classVersion, matchOn,mainCompanyId,branchId);
                // Set up cache options (e.g., 120 minutes sliding expiration)
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(120));

                // Cache this version separately
                _cache.Set(cacheKey, allData, cacheOptions);
            }

            // Paginate the cached data
            var pagedData = allData
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return pagedData;

       

        }

        private async Task<List<AuditReadDto>> GetAuditDtosWithBestBeforeOrPrevMonthAsync(
            string classTypeName,
            string matchOn,
            int mainCompanyId,
            int branchId,
            bool includeCancelled = true)
        {
            var useAllMatchingClasses = string.Equals(
                classTypeName,
                "ClassV10",
                StringComparison.OrdinalIgnoreCase
            );

            // ========================================================
            // Load ScriptItems
            // ========================================================
            var query = _context.ScriptItems
                .AsNoTracking()
                .Include(si => si.Script)
                    .ThenInclude(s => s.Branch)
                .Include(si => si.Drug)
                    .ThenInclude(d => d.DrugClasses)
                        .ThenInclude(dc => dc.ClassInfo)
                            .ThenInclude(ci => ci.ClassType)
                .Include(si => si.Insurance)
                    .ThenInclude(irx => irx.InsurancePCN)
                        .ThenInclude(pcn => pcn.Insurance)
                .Include(si => si.Prescriber)
                .Where(si =>
                    si.Script.Branch.MainCompanyId == mainCompanyId &&
                    si.Drug.DrugClasses.Any(dc =>
                        dc.ClassInfo.ClassType.Name == classTypeName
                    )
                );

            // Optional filter: hide cancelled if needed
            if (!includeCancelled)
            {
                query = query.Where(si =>
                    si.Status == null ||
                    si.Status != "Cancelled"
                );
            }

            // Optional branch filter:
            // If branchId > 0, return only this branch.
            // If branchId <= 0, return all branches for the company.
            //if (branchId > 0)
            //{
            //    query = query.Where(si => si.Script.BranchId == branchId);
            //}

            var scriptItems = await query.ToListAsync();

            if (!scriptItems.Any())
                return new List<AuditReadDto>();

            // ========================================================
            // Load ClassInsurances
            // ========================================================
            var classInsurances = await _context.ClassInsurances
                .AsNoTracking()
                .Include(ci => ci.Drug)
                .Include(ci => ci.Insurance)
                    .ThenInclude(irx => irx.InsurancePCN)
                        .ThenInclude(pcn => pcn.Insurance)
                .ToListAsync();

            // ========================================================
            // Group ClassInsurances by:
            // BranchId + selected match value + ClassInfoId
            // ========================================================
            var ciGroups = classInsurances
                .Where(ci => ci.Insurance?.InsurancePCN?.Insurance?.Bin != null)
                .GroupBy(ci =>
                {
                    string? matchValue = matchOn.ToUpperInvariant() switch
                    {
                        "BIN" => ci.Insurance?.InsurancePCN?.Insurance?.Bin,
                        "PCN" => ci.Insurance?.InsurancePCN?.PCN,
                        "RX" => ci.Insurance?.RxGroup,
                        _ => null
                    };

                    return new
                    {
                        ci.BranchId,
                        Match = matchValue,
                        ci.ClassInfoId
                    };
                })
                .Where(g => g.Key.Match != null)
                .ToDictionary(g => g.Key, g => g.ToList());

            // ========================================================
            // Remaining Stock dictionary for highest alternative
            // ========================================================
            var stockDict = scriptItems
                .GroupBy(si => (si.Script.ScriptCode, si.DrugId))
                .ToDictionary(g => g.Key, g => g.First().RemainingStock);

            var auditDtos = new List<AuditReadDto>();

            // ========================================================
            // Build DTOs
            // ========================================================
            foreach (var si in scriptItems)
            {
                var scriptDate = si.Script.Date.ToUniversalTime();
                var prevMonth = StartOfMonth(scriptDate).AddMonths(-1);

                var classInfoIdsForDrug = si.Drug.DrugClasses
                    .Where(dc => dc.ClassInfo.ClassType.Name == classTypeName)
                    .Select(dc => dc.ClassId)
                    .Distinct()
                    .ToList();

                if (!classInfoIdsForDrug.Any())
                    continue;

                var firstMatchingClass = si.Drug.DrugClasses
                    .First(dc => dc.ClassInfo.ClassType.Name == classTypeName);

                string? matchValue = matchOn.ToUpperInvariant() switch
                {
                    "BIN" => si.Insurance?.InsurancePCN?.Insurance?.Bin,
                    "PCN" => si.Insurance?.InsurancePCN?.PCN,
                    "RX" => si.Insurance?.RxGroup,
                    _ => null
                };

                if (matchValue == null)
                    continue;

                ClassInsurance? bestAlt = null;

                // ========================================================
                // Find Best Alternative
                // ========================================================
                if (useAllMatchingClasses)
                {
                    // ClassV10 behavior:
                    // Use ALL matching ClassInfoIds for this drug,
                    // then pick the best candidate.
                    var candidates = new List<ClassInsurance>();

                    foreach (var classInfoId in classInfoIdsForDrug)
                    {
                        var key = new
                        {
                            si.Script.BranchId,
                            Match = matchValue,
                            ClassInfoId = classInfoId
                        };

                        if (ciGroups.TryGetValue(key, out var ciList) && ciList != null)
                        {
                            candidates.AddRange(ciList);
                        }
                    }

                    if (candidates.Count > 0)
                    {
                        // First: previous month, highest BestNet
                        bestAlt = candidates
                            .Where(ci => StartOfMonth(ci.Date) == prevMonth)
                            .OrderByDescending(ci => ci.BestNet)
                            .ThenByDescending(ci => ci.Date)
                            .FirstOrDefault();

                        // Fallback: most recent <= script date, highest BestNet
                        bestAlt ??= candidates
                            .Where(ci => ci.Date <= scriptDate)
                            .OrderByDescending(ci => ci.Date)
                            .ThenByDescending(ci => ci.BestNet)
                            .FirstOrDefault();
                    }
                }
                else
                {
                    // Original behavior:
                    // Use only the first matching class.
                    var drugClassInfoId = firstMatchingClass.ClassId;

                    var key = new
                    {
                        si.Script.BranchId,
                        Match = matchValue,
                        ClassInfoId = drugClassInfoId
                    };

                    if (ciGroups.TryGetValue(key, out var ciList))
                    {
                        // First: previous month, highest BestNet
                        bestAlt = ciList
                            .Where(ci => StartOfMonth(ci.Date) == prevMonth)
                            .OrderByDescending(ci => ci.BestNet)
                            .FirstOrDefault();

                        // Fallback: most recent <= script date, then BestNet
                        bestAlt ??= ciList
                            .Where(ci => ci.Date <= scriptDate)
                            .OrderByDescending(ci => ci.Date)
                            .ThenByDescending(ci => ci.BestNet)
                            .FirstOrDefault();
                    }
                }

                // ========================================================
                // NP Verification Calculations
                // ========================================================
                var quantity = si.Quantity <= 0 ? 1 : si.Quantity;
                var calculatedNetProfit = si.NetProfit;
                var calculatedNetProfitPerItem = calculatedNetProfit / quantity;

                decimal? originalNetProfitPerItem = null;
                decimal? npDiscrepancyPerItem = null;
                string npComparisonStatus = "No Source NP";

                if (si.OriginalNetProfit.HasValue)
                {
                    originalNetProfitPerItem = si.OriginalNetProfit.Value / quantity;
                    npDiscrepancyPerItem = (si.NPDiscrepancy ?? 0) / quantity;

                    var absDiff = Math.Abs(si.NPDiscrepancy ?? 0);

                    npComparisonStatus = absDiff switch
                    {
                        <= 0.01m => "Matched",
                        <= 1.00m => "Small Difference",
                        _ => "Different"
                    };
                }

                // ========================================================
                // Main DTO
                // ========================================================
                var dto = new AuditReadDto
                {
                    // Current Script Info
                    Date = si.Script.Date,
                    ScriptCode = si.Script.ScriptCode,
                    RxNumber = si.RxNumber,
                    BranchCode = si.Script.Branch.Code,
                    BranchName = si.Script.Branch.Name,

                    // Current Drug Info
                    DrugId = si.DrugId,
                    DrugName = si.Drug.Name,
                    NDCCode = si.NDCCode,
                    DrugClass = firstMatchingClass.ClassInfo.Name,

                    // Current Insurance Info
                    InsuranceId = si.InsuranceId,
                    RxGroupId = si.Insurance?.Id ?? 0,
                    PcnId = si.Insurance?.InsurancePCN?.Id ?? 0,
                    BinId = si.Insurance?.InsurancePCN?.Insurance?.Id ?? 0,
                    InsuranceRx = si.Insurance?.RxGroup ?? "",
                    BINCode = si.Insurance?.InsurancePCN?.Insurance?.Bin ?? "",
                    BINName = si.Insurance?.InsurancePCN?.Insurance?.Name ?? "",
                    PCNName = si.Insurance?.InsurancePCN?.PCN ?? "",

                    // User / Prescriber
                    User = (si.UserEmail ?? "").Replace(".@pharmacy.com", ""),
                    Prescriber = si.Prescriber?.Name ?? "",

                    // Current Financial Values
                    PF = si.PF,
                    Quantity = si.Quantity,
                    RemainingStock = si.RemainingStock,
                    AcquisitionCost = si.AcquisitionCost,
                    Discount = si.Discount,
                    InsurancePayment = si.InsurancePayment,
                    PatientPayment = si.PatientPayment,
                    NetProfit = calculatedNetProfit,
                    NetProfitPerItem = calculatedNetProfitPerItem,

                    // Source NP Comparison
                    OriginalNetProfit = si.OriginalNetProfit,
                    OriginalNetProfitPerItem = originalNetProfitPerItem,
                    NPDiscrepancy = si.NPDiscrepancy,
                    NPDiscrepancyPerItem = npDiscrepancyPerItem,
                    NPComparisonStatus = npComparisonStatus,
                    GrossProfit = si.GrossProfit,

                    // Pricing Reference Fields
                    AWP = si.AWP ?? si.Drug.AWP,
                    WAC = si.WAC,
                    SDRA = si.SDRA,
                    ReimbursementRatePctOfAWP = ComputeReimbursementRate(si),

                    // Supply / Refill Fields
                    Refill = si.Refill,
                    DaySupply = si.DaySupply,
                    DaySupplyEndDate = si.DaySupplyEndDate,
                    RefillDate = si.RefillDate,
                    Unit = si.Unit,

                    // Status Fields
                    Status = si.Status,
                    RxStatus = si.RxStatus,
                    Priority = si.Priority
                };

                // ========================================================
                // Highest Alternative Mapping
                // ========================================================
                if (bestAlt != null)
                {
                    dto.HighestDrugId = bestAlt.DrugId;
                    dto.HighestDrugName = bestAlt.Drug?.Name ?? "";
                    dto.HighestDrugNDC = bestAlt.Drug?.NDC ?? "";

                    // Use current script quantity, not best alternative quantity.
                    dto.HighestNet = bestAlt.BestNet * quantity;

                    dto.HighestScriptCode = bestAlt.ScriptCode;
                    dto.HighestScriptDate = bestAlt.ScriptDateTime;
                    dto.HighestNetProfitPerItem = bestAlt.BestNet;
                    dto.HighestQuantity = bestAlt.Qty;

                    dto.HighestBINCode = bestAlt.Insurance?.InsurancePCN?.Insurance?.Bin ?? "";
                    dto.HighestBINName = bestAlt.Insurance?.InsurancePCN?.Insurance?.Name ?? "";
                    dto.HighestPCNName = bestAlt.Insurance?.InsurancePCN?.PCN ?? "";
                    dto.HighestInsuranceRx = bestAlt.Insurance?.RxGroup ?? "";

                    dto.HighestRxGroupId = bestAlt.InsuranceId;
                    dto.HighestPcnId = bestAlt.Insurance?.InsurancePCN?.Id ?? 0;
                    dto.HighestBinId = bestAlt.Insurance?.InsurancePCN?.Insurance?.Id?? 0;

                    stockDict.TryGetValue(
                        (bestAlt.ScriptCode, bestAlt.DrugId),
                        out int altRemainingStock
                    );

                    dto.HighestRemainingStock = altRemainingStock;
                }

                auditDtos.Add(dto);
            }

            return auditDtos;
        }
        /// <summary>
        /// Computes what percentage of AWP the insurance reimbursed.
        /// Returns null if AWP is not available or zero.
        /// Useful for benchmarking PBM contracts:
        ///   high % = strong contract, low % = weak contract.
        /// </summary>
        private static decimal? ComputeReimbursementRate(ScriptItem si)
        {
            var awp = si.AWP ?? si.Drug?.AWP ?? 0;

            if (awp <= 0) return null;
            if (si.InsurancePayment <= 0) return null;

            // Insurance payment as a percentage of AWP × Quantity
            var totalAWP = awp * si.Quantity;
            if (totalAWP <= 0) return null;

            return Math.Round((si.InsurancePayment / totalAWP) * 100m, 2);
        }



        private static DateTime StartOfMonth(DateTime dt)
        {
            dt = dt.ToUniversalTime();
            return new DateTime(dt.Year, dt.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        }



        private async Task<List<AuditReadDto>> LoadAllLatestScriptsFromDatabaseAsync(string classType = "ClassV1")
        {
            var auditData = await (
                from script in _context.Scripts
                join scriptItem in _context.ScriptItems on script.Id equals scriptItem.ScriptId
                join insurance in _context.InsuranceRxes on scriptItem.InsuranceId equals insurance.Id into insuranceGroup
                from insurance in insuranceGroup.DefaultIfEmpty()
                join drug in _context.Drugs on scriptItem.DrugId equals drug.Id
                join drugClass in _context.DrugClasses on drug.Id equals drugClass.DrugId
                join classItem in _context.ClassInfos on drugClass.ClassId equals classItem.Id into classGroup
                from classItem in classGroup.DefaultIfEmpty()
                join classTypeEntity in _context.ClassTypes on classItem.ClassTypeId equals classTypeEntity.Id into classTypeGroup
                from classTypeEntity in classTypeGroup.DefaultIfEmpty()
                join branch in _context.Branches on script.BranchId equals branch.Id into branchGroup
                from branch in branchGroup.DefaultIfEmpty()
                join classInsurance in _context.ClassInsurances
                    on new
                    {
                        InsuranceId = insurance != null ? (int?)insurance.Id : null,
                        ClassId = classItem != null ? (int?)classItem.Id : null,
                        Year = script.Date.Year,
                        Month = script.Date.Month,
                        BranchId = branch != null ? (int?)branch.Id : null
                    }
                    equals new
                    {
                        InsuranceId = (int?)classInsurance.InsuranceId,
                        ClassId = (int?)classInsurance.ClassInfoId,
                        Year = classInsurance.Date.Year,
                        Month = classInsurance.Date.Month,
                        BranchId = (int?)classInsurance.BranchId
                    }
                    into classInsuranceGroup
                from classInsurance in classInsuranceGroup.DefaultIfEmpty()
                join bestDrug in _context.Drugs on classInsurance.DrugId equals bestDrug.Id into bestDrugGroup
                from bestDrug in bestDrugGroup.DefaultIfEmpty()
                join user in _context.Users on script.UserId equals user.Id into userGroup
                from user in userGroup.DefaultIfEmpty()
                join prescriber in _context.Users on scriptItem.UserEmail equals prescriber.Email into prescriberGroup
                from prescriber in prescriberGroup.DefaultIfEmpty()
                    // Filter by ClassType.Name
                where classItem == null || classTypeEntity.Name == classType
                let prevMonth = script.Date.AddMonths(-1)
                let bestNetEntryPrevMonth = _context.ClassInsurances
                    .Where(ci => ci.InsuranceId == (insurance != null ? insurance.Id : 0) &&
                                 ci.ClassInfoId == (classItem != null ? classItem.Id : 0) &&
                                 ci.Date.Year == prevMonth.Year &&
                                 ci.Date.Month == prevMonth.Month)
                    .OrderByDescending(ci => ci.BestNet)
                    .FirstOrDefault()
                let bestNetEntryCurrentMonth = _context.ClassInsurances
                    .Where(ci => ci.InsuranceId == (insurance != null ? insurance.Id : 0) &&
                                 ci.ClassInfoId == (classItem != null ? classItem.Id : 0) &&
                                 ci.Date.Year == script.Date.Year &&
                                 ci.Date.Month == script.Date.Month)
                    .OrderByDescending(ci => ci.BestNet)
                    .FirstOrDefault()
                let bestNetEntry = bestNetEntryPrevMonth ?? bestNetEntryCurrentMonth
                let bestScript = bestNetEntryPrevMonth != null
                    ? _context.Scripts.FirstOrDefault(s => s.ScriptCode == bestNetEntry.ScriptCode)
                    : null
                select new AuditReadDto
                {
                    RemainingStock = scriptItem.RemainingStock,
                    HighestRemainingStock = scriptItem.RemainingStock,
                    Date = script.Date,
                    ScriptCode = script.ScriptCode,
                    RxNumber = scriptItem.RxNumber,
                    User = user != null ? user.Name : "Unknown",
                    DrugName = drug != null ? drug.Name : null,
                    NDCCode = drug != null ? drug.NDC : null,
                    DrugId = drug != null ? drug.Id : 0,
                    HighestDrugName = bestDrug != null ? bestDrug.Name : null,
                    HighestDrugNDC = bestDrug != null ? bestDrug.NDC : null,
                    HighestDrugId = bestDrug != null ? bestDrug.Id : 0,
                    BranchCode = branch != null ? branch.Name : null,
                    InsuranceRx = insurance != null ? insurance.RxGroup : null,
                    InsuranceId = insurance != null ? insurance.Id : 0,
                    PF = scriptItem.PF,
                    Prescriber = prescriber != null ? prescriber.Name : "Unknown",
                    Quantity = scriptItem.Quantity,
                    AcquisitionCost = scriptItem.AcquisitionCost,
                    Discount = scriptItem.Discount,
                    InsurancePayment = scriptItem.InsurancePayment,
                    PatientPayment = scriptItem.PatientPayment,
                    NetProfit = scriptItem.NetProfit,
                    DrugClass = classItem != null ? classItem.Name : null,
                    HighestNet = bestNetEntry != null ? bestNetEntry.BestNet : 0,
                    HighestScriptCode = bestNetEntry != null ? bestNetEntry.ScriptCode : null,
                    HighestScriptDate = bestNetEntry != null ? bestNetEntry.ScriptDateTime : DateTime.MinValue
                }
            ).ToListAsync();

            return auditData;
        }

        internal async Task<ICollection<DrugInsuranceReadDto>> GetInsuranceByNdc(string ndc)
        {
            var result = await (
                from di in _context.DrugInsurances
                join ins in _context.InsuranceRxes on di.InsuranceId equals ins.Id into insuranceJoin
                from ins in insuranceJoin.DefaultIfEmpty() // Left Join to handle nulls
                join drg in _context.Drugs on di.DrugId equals drg.Id into drugJoin
                from drg in drugJoin.DefaultIfEmpty()
                join br in _context.Branches on di.BranchId equals br.Id into branchJoin
                from br in branchJoin.DefaultIfEmpty()
                where di.NDCCode == ndc
                select new DrugInsuranceReadDto
                {
                    InsuranceId = ins.Id,
                    DrugId = drg.Id,
                    BranchId = br.Id,
                    NDCCode = di.NDCCode,
                    Net = di.Net,
                    date = di.Date,
                    Prescriber = di.Prescriber,
                    Quantity = di.Quantity,
                    AcquisitionCost = di.AcquisitionCost,
                    Discount = di.Discount,
                    InsurancePayment = di.InsurancePayment,
                    PatientPayment = di.PatientPayment,
                    Insurance = ins != null ? ins.RxGroup : null,
                    Drug = drg != null ? drg.Name : null,
                    Branch = br != null ? br.Name : null,
                    Id = di.Id
                }
            ).ToListAsync();

            return result;
        }



        internal async Task<Script> GetScriptAsync(string scriptCode)
        {
            var item = await _context.Scripts.FirstOrDefaultAsync(x => x.ScriptCode == scriptCode);
            return item;
        }
        internal async Task<ICollection<ScriptItemDto>> GetScriptByScriptCode(string scriptCode)
        {
            var items = await (
                from script in _context.Scripts
                join scriptItem in _context.ScriptItems on script.Id equals scriptItem.ScriptId
                join branch in _context.Branches on script.BranchId equals branch.Id
                join drug in _context.Drugs on scriptItem.DrugId equals drug.Id
                join insurance in _context.InsuranceRxes on scriptItem.InsuranceId equals insurance.Id
                join prescriber in _context.Users on scriptItem.UserEmail equals prescriber.Email into prescriberGroup
                join user in _context.Users on script.UserId equals user.Id
                from prescriber in prescriberGroup.DefaultIfEmpty() // Allow null prescriber

                where script.ScriptCode == scriptCode

                select new ScriptItemDto
                {
                    Id = scriptItem.Id,
                    DrugName = drug.Name,
                    NDCCode = scriptItem.NDCCode,
                    Quantity = scriptItem.Quantity,
                    PF = scriptItem.PF,
                    InsuranceName = insurance.RxGroup,
                    PrescriberName = prescriber != null ? prescriber.Name : "Unknown",
                    UserName = user.Name,
                    AcquisitionCost = scriptItem.AcquisitionCost,
                    Discount = scriptItem.Discount,
                    InsurancePayment = scriptItem.InsurancePayment,
                    PatientPayment = scriptItem.PatientPayment,
                    BranchName = branch.Name,
                    Date = script.Date.ToString("MM-dd-yyyy"),
                }
            ).ToListAsync();

            return items;
        }

        public async Task ImportInsurancesFromCsvAsync(string filePath = "insurance.csv")
        {
            List<InsuranceCsvRecord> csvRecords;

            // Read the CSV file
            using (var reader = new StreamReader(filePath))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                csv.Context.RegisterClassMap<InsuranceCsvMap>(); // Register the corrected mapping
                csvRecords = csv.GetRecords<InsuranceCsvRecord>().ToList();
            }

            // Fetch all existing insurances from DB and index them by BIN
            var insuranceDic = (await _context.Insurances.ToListAsync())
                .ToDictionary(x => x.Bin, StringComparer.OrdinalIgnoreCase);

            foreach (var csvRecord in csvRecords)
            {
                // Ensure Full Name is at least 6 characters long by padding with leading zeros
                if (csvRecord.Bin.Length < 6)
                {
                    csvRecord.Bin = csvRecord.Bin.PadLeft(6, '0');
                }

                // Skip if BIN is empty or invalid
                if (string.IsNullOrWhiteSpace(csvRecord.Bin))
                {
                    continue;
                }

                // Search in the database by BIN and update Full Name
                if (insuranceDic.TryGetValue(csvRecord.Bin, out var existingInsurance))
                {
                    existingInsurance.Name = csvRecord.FullName;
                }
            }

            await _context.SaveChangesAsync();
        }

        // CSV model with correct header mapping
        public class InsuranceCsvRecord
        {
            public string Bin { get; set; }
            public string FullName { get; set; }
        }

        // Mapping class for CsvHelper
        public sealed class InsuranceCsvMap : ClassMap<InsuranceCsvRecord>
        {
            public InsuranceCsvMap()
            {
                Map(m => m.Bin).Name("BIN");
                Map(m => m.FullName).Name("Full Name");
            }
        }




        internal async Task<ICollection<Drug>> GetDrugsByClassBranch(int classId, int branchId)
        {
            var items = await (
                from drug in _context.Drugs

                join drugClass in _context.DrugClasses
                    on drug.Id equals drugClass.DrugId
                join classInfo in _context.ClassInfos
                    on drugClass.ClassId equals classInfo.Id

                join db in _context.DrugBranches
                    on drug.NDC equals db.DrugNDC

                where classInfo.Id == classId && db.BranchId == branchId

                select drug
            ).ToListAsync();

            return items;
        }


        internal async Task<ICollection<Drug>> GetDrugsByInsurance(int insuranceId, string drug)
        {
            var drugs = await (from d in _context.Drugs
                               join di in _context.DrugInsurances on d.Id equals di.DrugId
                               where di.InsuranceId == insuranceId &&
                                     d.Name.ToLower().Contains(drug.ToLower())
                               select d)
                              .Distinct()
                              .ToListAsync();

            return drugs;
        }
        internal async Task<ICollection<Drug>> GetDrugsByInsurance(string insurance)
        {
            var drugs = await _context.DrugInsurances
                .Include(di => di.Drug)
                .Include(di => di.Insurance)
                .Where(di => di.Insurance != null && di.Insurance.RxGroup.ToLower() == insurance.ToLower())
                .Select(di => di.Drug)
                .Distinct()
                .ToListAsync();

            return drugs;
        }
        // ...existing code...
        internal async Task<ICollection<Drug>> GetDrugsByInsuranceNameDrugName(string insurance, string drugName, int pageSize = 1000, int pageNumber = 1)
        {
            Console.WriteLine($"Searching for drugs with insurance: {insurance} and drug name: {drugName} on page {pageNumber} with page size {pageSize}");
            var drugs = await _context.DrugInsurances
                .Include(di => di.Drug)
                .Include(di => di.Insurance)
                .Where(di => di.Insurance != null && di.Insurance.RxGroup.ToLower() == insurance.ToLower() &&
                             di.Drug.Name.ToLower().Contains(drugName.ToLower()))
                .Select(di => di.Drug)
                .Distinct()
                .OrderBy(d => d.Id) // Ensure deterministic ordering for paging
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return drugs;
        }
        // ...existing code...

        internal async Task<ICollection<Drug>> GetDrugsByPCN(string pcn)
        {
            var drugs = await _context.DrugInsurances
                .Include(di => di.Drug) // Load Drug
                .Include(di => di.Insurance) // Load InsuranceRx
                    .ThenInclude(ir => ir.InsurancePCN) // Load InsurancePCN for access to PCN
                .Where(di => di.Insurance != null
                             && di.Insurance.InsurancePCN != null
                             && di.Insurance.InsurancePCN.PCN.ToLower() == pcn.ToLower())
                .Select(di => di.Drug)
                .Distinct() // Avoid duplicate drugs if multiple records exist
                .ToListAsync();

            return drugs;
        }
        internal async Task<ICollection<Drug>> GetDrugsByBIN(string bin)
        {
            var drugs = await _context.DrugInsurances
                .Include(di => di.Drug) // Load Drug
                .Include(di => di.Insurance) // Load InsuranceRx
                    .ThenInclude(ir => ir.InsurancePCN) // Load InsurancePCN for access to PCN
                .Where(di => di.Insurance != null
                             && di.Insurance.InsurancePCN.Insurance.Bin != null
                             && di.Insurance.InsurancePCN.Insurance.Bin.ToLower() == bin.ToLower())
                .Select(di => di.Drug)
                .Distinct() // Avoid duplicate drugs if multiple records exist
                .ToListAsync();

            return drugs;
        }

        internal async Task<ICollection<Insurance>> GetInsurances(string insurance)
        {
            var items = await _context.Insurances.Where(x => x.Name.ToLower().Contains(insurance.ToLower())).ToListAsync();
            return items;
        }

        internal async Task<ICollection<Insurance>> GetInsurancesBinsByName(string bin)
        {
            var items = await _context.Insurances
                .Where(x => x.Bin.ToLower().Contains(bin.ToLower()) || x.Name.ToLower().Contains(bin.ToLower()))
                .ToListAsync();
            return items;
        }

        internal async Task<ICollection<InsurancePCN>> GetInsurancesPcnByBinId(int binId)
        {
            var items = await _context.InsurancePCNs.Where(x => x.InsuranceId == binId).ToListAsync();
            return items;
        }
        internal async Task<ICollection<InsuranceRx>> GetInsurancesRxByPcnId(int pcnId)
        {
            var items = await _context.InsuranceRxes.Where(x => x.InsurancePCNId == pcnId).ToListAsync();
            return items;
        }
        internal async Task<ICollection<DrugMediReadDto>> GetAllMediDrugs(int classId)
        {
            var items = await (
                from drugmedi in _context.DrugMedis
                join drug in _context.Drugs on drugmedi.DrugId equals drug.Id
                join drugClass in _context.DrugClasses on drug.Id equals drugClass.DrugId
                join classInfo in _context.ClassInfos on drugClass.ClassId equals classInfo.Id
                where classInfo.Id == classId
                select new { drugmedi, drug }
            ).ToListAsync();

            var result = items.Select(item =>
            {
                var dto = _mapper.Map<DrugMediReadDto>(item.drugmedi);
                dto.DrugName = item.drug.Name;
                dto.DrugNDC = item.drug.NDC;
                return dto;
            }).ToList();

            return result;
        }



        internal async Task<ICollection<Drug>> GetDrugsByInsuranceNamePaginated(
            string insurance, string drugName, int pageSize, int pageNumber, bool isDemo = false)
        {
            // Normalize paging
            pageSize = Math.Max(pageSize, 1);
            pageNumber = Math.Max(pageNumber, 1);
            int offset = (pageNumber - 1) * pageSize;

            // Enable fuzzy search for this session (pg_trgm)
            await _context.Database.ExecuteSqlRawAsync("SET pg_trgm.similarity_threshold = 0.3;");

            // Decide limited-by-RxGroup or global:
            bool useGlobal = string.IsNullOrWhiteSpace(insurance);
            if (!useGlobal)
            {
                string rxNorm = insurance.Trim().ToLower();
                useGlobal = !await _context.DrugInsurances
                    .Join(_context.InsuranceRxes,
                          di => di.InsuranceId,
                          rx => rx.Id,
                          (di, rx) => new { rx.RxGroup, di.ScriptCode })
                    .AnyAsync(x =>
                        x.RxGroup.ToLower() == rxNorm &&
                        x.ScriptCode != null &&
                        x.ScriptCode != string.Empty);
            }

            int demoLimit = isDemo ? DemoDrugLimit : int.MaxValue;

            FormattableString sql;

            if (useGlobal)
            {
                sql = $@"
WITH source_drugs AS (
    SELECT *
    FROM ""Drugs""
    ORDER BY ""Id""
    LIMIT {demoLimit}
),
ranked AS (
    SELECT d.*,
           ROW_NUMBER() OVER (
               ORDER BY (
                   similarity(d.""name_unaccent"", unaccent({drugName})) * 0.5 +
                   ts_rank(d.""name_tsv"", plainto_tsquery(unaccent({drugName}))) * 0.3 +
                   CASE WHEN d.""name_soundex"" = soundex(unaccent({drugName})) THEN 0.1 ELSE 0 END +
                   CASE WHEN d.""name_unaccent"" ILIKE '%' || unaccent({drugName}) || '%' THEN 0.1 ELSE 0 END
               ) DESC
           ) AS rn,
           similarity(d.""name_unaccent"", unaccent({drugName})) AS sim,
           ts_rank(d.""name_tsv"", plainto_tsquery(unaccent({drugName}))) AS ts_rank
    FROM source_drugs d
    WHERE
        (
            d.""name_unaccent"" % unaccent({drugName}) OR
            d.""name_tsv"" @@ plainto_tsquery(unaccent({drugName})) OR
            d.""name_soundex"" = soundex(unaccent({drugName})) OR
            d.""name_unaccent"" ILIKE '%' || unaccent({drugName}) || '%'
        )
        AND EXISTS (
            SELECT 1
            FROM ""DrugInsurances"" di
            WHERE di.""DrugId"" = d.""Id""
              AND di.""ScriptCode"" IS NOT NULL
              AND di.""ScriptCode"" <> ''
        )
)
SELECT *
FROM ranked
ORDER BY sim DESC, ts_rank DESC
LIMIT {pageSize} OFFSET {offset};";
            }
            else
            {
                sql = $@"
WITH source_drugs AS (
    SELECT *
    FROM ""Drugs""
    ORDER BY ""Id""
    LIMIT {demoLimit}
),
ranked AS (
    SELECT d.*,
           ROW_NUMBER() OVER (
               ORDER BY (
                   similarity(d.""name_unaccent"", unaccent({drugName})) * 0.5 +
                   ts_rank(d.""name_tsv"", plainto_tsquery(unaccent({drugName}))) * 0.3 +
                   CASE WHEN d.""name_soundex"" = soundex(unaccent({drugName})) THEN 0.1 ELSE 0 END +
                   CASE WHEN d.""name_unaccent"" ILIKE '%' || unaccent({drugName}) || '%' THEN 0.1 ELSE 0 END
               ) DESC
           ) AS rn,
           similarity(d.""name_unaccent"", unaccent({drugName})) AS sim,
           ts_rank(d.""name_tsv"", plainto_tsquery(unaccent({drugName}))) AS ts_rank
    FROM source_drugs d
    WHERE
        (
            d.""name_unaccent"" % unaccent({drugName}) OR
            d.""name_tsv"" @@ plainto_tsquery(unaccent({drugName})) OR
            d.""name_soundex"" = soundex(unaccent({drugName})) OR
            d.""name_unaccent"" ILIKE '%' || unaccent({drugName}) || '%'
        )
        AND EXISTS (
            SELECT 1
            FROM ""DrugInsurances"" di
            JOIN ""InsuranceRxes"" rx ON rx.""Id"" = di.""InsuranceId""
            WHERE di.""DrugId"" = d.""Id""
              AND di.""ScriptCode"" IS NOT NULL
              AND di.""ScriptCode"" <> ''
              AND LOWER(rx.""RxGroup"") = LOWER({insurance})
        )
)
SELECT *
FROM ranked
ORDER BY sim DESC, ts_rank DESC
LIMIT {pageSize} OFFSET {offset};";
            }

            var drugs = await _context.Drugs
                .FromSqlInterpolated(sql)
                .AsNoTracking()
                .ToListAsync();

            if (!useGlobal && drugs.Count == 0)
            {
                FormattableString fallbackSql = $@"
WITH source_drugs AS (
    SELECT *
    FROM ""Drugs""
    ORDER BY ""Id""
    LIMIT {demoLimit}
),
ranked AS (
    SELECT d.*,
           ROW_NUMBER() OVER (
               ORDER BY (
                   similarity(d.""name_unaccent"", unaccent({drugName})) * 0.5 +
                   ts_rank(d.""name_tsv"", plainto_tsquery(unaccent({drugName}))) * 0.3 +
                   CASE WHEN d.""name_soundex"" = soundex(unaccent({drugName})) THEN 0.1 ELSE 0 END +
                   CASE WHEN d.""name_unaccent"" ILIKE '%' || unaccent({drugName}) || '%' THEN 0.1 ELSE 0 END
               ) DESC
           ) AS rn,
           similarity(d.""name_unaccent"", unaccent({drugName})) AS sim,
           ts_rank(d.""name_tsv"", plainto_tsquery(unaccent({drugName}))) AS ts_rank
    FROM source_drugs d
    WHERE
        (
            d.""name_unaccent"" % unaccent({drugName}) OR
            d.""name_tsv"" @@ plainto_tsquery(unaccent({drugName})) OR
            d.""name_soundex"" = soundex(unaccent({drugName})) OR
            d.""name_unaccent"" ILIKE '%' || unaccent({drugName}) || '%'
        )
        AND EXISTS (
            SELECT 1
            FROM ""DrugInsurances"" di
            WHERE di.""DrugId"" = d.""Id""
              AND di.""ScriptCode"" IS NOT NULL
              AND di.""ScriptCode"" <> ''
        )
)
SELECT *
FROM ranked
ORDER BY sim DESC, ts_rank DESC
LIMIT {pageSize} OFFSET {offset};";

                drugs = await _context.Drugs
                    .FromSqlInterpolated(fallbackSql)
                    .AsNoTracking()
                    .ToListAsync();
            }

            return drugs;
        }

        internal async Task<ICollection<Drug>> GetDrugsByPCNPaginated(
            string insurance, string drugName, int pageSize, int pageNumber, bool isDemo = false)
        {
            // Normalize paging
            pageSize = Math.Max(pageSize, 1);
            pageNumber = Math.Max(pageNumber, 1);
            int offset = (pageNumber - 1) * pageSize;

            // Enable fuzzy search for this session
            await _context.Database.ExecuteSqlRawAsync("SET pg_trgm.similarity_threshold = 0.2;");

            // Decide whether to use PCN-limited search or fallback to global
            bool useGlobal = string.IsNullOrWhiteSpace(insurance);

            if (!useGlobal)
            {
                string pcnNorm = insurance.Trim().ToLower();

                // Only consider PCNs that actually have DrugInsurances WITH ScriptCode present
                useGlobal = !await _context.DrugInsurances
                    .Join(_context.InsuranceRxes,
                          di => di.InsuranceId,
                          rx => rx.Id,
                          (di, rx) => new { di, rx })
                    .Join(_context.InsurancePCNs,
                          x => x.rx.InsurancePCNId,
                          p => p.Id,
                          (x, p) => new { p.PCN, x.di.ScriptCode })
                    .AnyAsync(x =>
                        x.PCN.ToLower() == pcnNorm &&
                        x.ScriptCode != null && x.ScriptCode != "");
            }

            int demoLimit = isDemo ? DemoDrugLimit : int.MaxValue;

            FormattableString sql;

            if (useGlobal)
            {
                sql = $@"
WITH source_drugs AS (
    SELECT *
    FROM ""Drugs""
    ORDER BY ""Id""
    LIMIT {demoLimit}
),
ranked AS (
    SELECT d.*,
           ROW_NUMBER() OVER (
               ORDER BY (
                   similarity(d.""name_unaccent"", unaccent({drugName})) * 0.5 +
                   ts_rank(d.""name_tsv"", plainto_tsquery(unaccent({drugName}))) * 0.3 +
                   CASE WHEN d.""name_soundex"" = soundex(unaccent({drugName})) THEN 0.1 ELSE 0 END +
                   CASE WHEN d.""name_unaccent"" ILIKE '%' || unaccent({drugName}) || '%' THEN 0.1 ELSE 0 END
               ) DESC
           ) AS rn,
           similarity(d.""name_unaccent"", unaccent({drugName})) AS sim,
           ts_rank(d.""name_tsv"", plainto_tsquery(unaccent({drugName}))) AS ts_rank
    FROM source_drugs d
    WHERE
        (
            d.""name_unaccent"" % unaccent({drugName}) OR
            d.""name_tsv"" @@ plainto_tsquery(unaccent({drugName})) OR
            d.""name_soundex"" = soundex(unaccent({drugName})) OR
            d.""name_unaccent"" ILIKE '%' || unaccent({drugName}) || '%'
        )
        AND EXISTS (
            SELECT 1
            FROM ""DrugInsurances"" di
            WHERE di.""DrugId"" = d.""Id""
              AND di.""ScriptCode"" IS NOT NULL
              AND di.""ScriptCode"" <> ''
        )
)
SELECT *
FROM ranked
ORDER BY sim DESC, ts_rank DESC
LIMIT {pageSize} OFFSET {offset};";
            }
            else
            {
                sql = $@"
WITH source_drugs AS (
    SELECT *
    FROM ""Drugs""
    ORDER BY ""Id""
    LIMIT {demoLimit}
),
ranked AS (
    SELECT d.*,
           ROW_NUMBER() OVER (
               ORDER BY (
                   similarity(d.""name_unaccent"", unaccent({drugName})) * 0.5 +
                   ts_rank(d.""name_tsv"", plainto_tsquery(unaccent({drugName}))) * 0.3 +
                   CASE WHEN d.""name_soundex"" = soundex(unaccent({drugName})) THEN 0.1 ELSE 0 END +
                   CASE WHEN d.""name_unaccent"" ILIKE '%' || unaccent({drugName}) || '%' THEN 0.1 ELSE 0 END
               ) DESC
           ) AS rn,
           similarity(d.""name_unaccent"", unaccent({drugName})) AS sim,
           ts_rank(d.""name_tsv"", plainto_tsquery(unaccent({drugName}))) AS ts_rank
    FROM ""DrugInsurances"" di
    INNER JOIN source_drugs d ON di.""DrugId"" = d.""Id""
    INNER JOIN ""InsuranceRxes"" rx ON di.""InsuranceId"" = rx.""Id""
    INNER JOIN ""InsurancePCNs"" pcn ON rx.""InsurancePCNId"" = pcn.""Id""
    WHERE LOWER(pcn.""PCN"") = LOWER({insurance})
      AND di.""ScriptCode"" IS NOT NULL
      AND di.""ScriptCode"" <> ''
      AND (
          d.""name_unaccent"" % unaccent({drugName}) OR
          d.""name_tsv"" @@ plainto_tsquery(unaccent({drugName})) OR
          d.""name_soundex"" = soundex(unaccent({drugName})) OR
          d.""name_unaccent"" ILIKE '%' || unaccent({drugName}) || '%'
      )
)
SELECT *
FROM ranked
ORDER BY sim DESC, ts_rank DESC
LIMIT {pageSize} OFFSET {offset};";
            }

            var drugs = await _context.Drugs
                .FromSqlInterpolated(sql)
                .AsNoTracking()
                .ToListAsync();

            // Safety net: if limited query returned zero, fallback once to global
            if (!useGlobal && drugs.Count == 0)
            {
                FormattableString fallbackSql = $@"
WITH source_drugs AS (
    SELECT *
    FROM ""Drugs""
    ORDER BY ""Id""
    LIMIT {demoLimit}
),
ranked AS (
    SELECT d.*,
           ROW_NUMBER() OVER (
               ORDER BY (
                   similarity(d.""name_unaccent"", unaccent({drugName})) * 0.5 +
                   ts_rank(d.""name_tsv"", plainto_tsquery(unaccent({drugName}))) * 0.3 +
                   CASE WHEN d.""name_soundex"" = soundex(unaccent({drugName})) THEN 0.1 ELSE 0 END +
                   CASE WHEN d.""name_unaccent"" ILIKE '%' || unaccent({drugName}) || '%' THEN 0.1 ELSE 0 END
               ) DESC
           ) AS rn,
           similarity(d.""name_unaccent"", unaccent({drugName})) AS sim,
           ts_rank(d.""name_tsv"", plainto_tsquery(unaccent({drugName}))) AS ts_rank
    FROM source_drugs d
    WHERE
        (
            d.""name_unaccent"" % unaccent({drugName}) OR
            d.""name_tsv"" @@ plainto_tsquery(unaccent({drugName})) OR
            d.""name_soundex"" = soundex(unaccent({drugName})) OR
            d.""name_unaccent"" ILIKE '%' || unaccent({drugName}) || '%'
        )
        AND EXISTS (
            SELECT 1
            FROM ""DrugInsurances"" di
            WHERE di.""DrugId"" = d.""Id""
              AND di.""ScriptCode"" IS NOT NULL
              AND di.""ScriptCode"" <> ''
        )
)
SELECT *
FROM ranked
ORDER BY sim DESC, ts_rank DESC
LIMIT {pageSize} OFFSET {offset};";

                drugs = await _context.Drugs
                    .FromSqlInterpolated(fallbackSql)
                    .AsNoTracking()
                    .ToListAsync();
            }

            return drugs;
        }


        internal async Task<ICollection<Drug>> GetDrugsByBINPaginated(
          string insurance, string drugName, int pageSize, int pageNumber, bool isDemo = false)
        {
            // Normalize paging
            pageSize = Math.Max(pageSize, 1);
            pageNumber = Math.Max(pageNumber, 1);
            int offset = (pageNumber - 1) * pageSize;

            // Fuzzy search threshold for this session
            await _context.Database.ExecuteSqlRawAsync("SET pg_trgm.similarity_threshold = 0.2;");

            // Decide whether to use BIN-limited search or fallback to global
            bool useGlobal = string.IsNullOrWhiteSpace(insurance);

            if (!useGlobal)
            {
                string binNorm = insurance.Trim().ToLower();

                // Use BIN-limited path only if there is at least one DI row for this BIN WITH ScriptCode present
                useGlobal = !await _context.DrugInsurances
                    .Join(_context.InsuranceRxes,
                          di => di.InsuranceId,
                          rx => rx.Id,
                          (di, rx) => new { di, rx })
                    .Join(_context.InsurancePCNs,
                          x => x.rx.InsurancePCNId,
                          pcn => pcn.Id,
                          (x, pcn) => new { x.di, x.rx, pcn })
                    .Join(_context.Insurances,
                          x => x.pcn.InsuranceId,
                          i => i.Id,
                          (x, i) => new { i.Bin, x.di.ScriptCode })
                    .AnyAsync(x =>
                        x.Bin.ToLower() == binNorm &&
                        x.ScriptCode != null && x.ScriptCode != "");
            }

            FormattableString sql;

            if (useGlobal)
            {
                // GLOBAL: only drugs that have at least one DrugInsurance with non-empty ScriptCode
                sql = $@"
WITH ranked AS (
    SELECT d.*,
           CAST(0 AS numeric) AS ""Net"",
           CAST(0 AS numeric) AS ""InsurancePayment"",
           ROW_NUMBER() OVER (
               ORDER BY (
                   similarity(d.""name_unaccent"", unaccent({drugName})) * 0.5 +
                   ts_rank(d.""name_tsv"", plainto_tsquery(unaccent({drugName}))) * 0.3 +
                   CASE WHEN d.""name_soundex"" = soundex(unaccent({drugName})) THEN 0.1 ELSE 0 END +
                   CASE WHEN d.""name_unaccent"" ILIKE '%' || unaccent({drugName}) || '%' THEN 0.1 ELSE 0 END
               ) DESC,
               1
           ) AS rn,
           similarity(d.""name_unaccent"", unaccent({drugName})) AS sim,
           ts_rank(d.""name_tsv"", plainto_tsquery(unaccent({drugName}))) AS ts_rank
    FROM ""Drugs"" d
    WHERE
        (
            d.""name_unaccent"" % unaccent({drugName}) OR
            d.""name_tsv"" @@ plainto_tsquery(unaccent({drugName})) OR
            d.""name_soundex"" = soundex(unaccent({drugName})) OR
            d.""name_unaccent"" ILIKE '%' || unaccent({drugName}) || '%'
        )
        AND EXISTS (
            SELECT 1
            FROM ""DrugInsurances"" di
            WHERE di.""DrugId"" = d.""Id""
              AND di.""ScriptCode"" IS NOT NULL
              AND di.""ScriptCode"" <> ''
        )
)
SELECT *
FROM ranked
ORDER BY sim DESC, ts_rank DESC, ""Net"" DESC, ""InsurancePayment"" DESC
LIMIT {pageSize} OFFSET {offset};";
            }
            else
            {
                // BIN-limited: require ScriptCode on the joined DI row
                sql = $@"
WITH ranked AS (
    SELECT d.*,
           di.""Net"",
           di.""InsurancePayment"",
           ROW_NUMBER() OVER (
               ORDER BY (
                   similarity(d.""name_unaccent"", unaccent({drugName})) * 0.5 +
                   ts_rank(d.""name_tsv"", plainto_tsquery(unaccent({drugName}))) * 0.3 +
                   CASE WHEN d.""name_soundex"" = soundex(unaccent({drugName})) THEN 0.1 ELSE 0 END +
                   CASE WHEN d.""name_unaccent"" ILIKE '%' || unaccent({drugName}) || '%' THEN 0.1 ELSE 0 END
               ) DESC,
               di.""Net"" DESC,
               di.""InsurancePayment"" DESC
           ) AS rn,
           similarity(d.""name_unaccent"", unaccent({drugName})) AS sim,
           ts_rank(d.""name_tsv"", plainto_tsquery(unaccent({drugName}))) AS ts_rank
    FROM ""DrugInsurances"" di
    INNER JOIN ""Drugs"" d ON di.""DrugId"" = d.""Id""
    INNER JOIN ""InsuranceRxes"" rx ON di.""InsuranceId"" = rx.""Id""
    INNER JOIN ""InsurancePCNs"" pcn ON rx.""InsurancePCNId"" = pcn.""Id""
    INNER JOIN ""Insurances"" i ON pcn.""InsuranceId"" = i.""Id""
    WHERE LOWER(i.""Bin"") = LOWER({insurance})
      AND di.""ScriptCode"" IS NOT NULL
      AND di.""ScriptCode"" <> ''
      AND (
          d.""name_unaccent"" % unaccent({drugName}) OR
          d.""name_tsv"" @@ plainto_tsquery(unaccent({drugName})) OR
          d.""name_soundex"" = soundex(unaccent({drugName})) OR
          d.""name_unaccent"" ILIKE '%' || unaccent({drugName}) || '%'
      )
)
SELECT *
FROM ranked
ORDER BY sim DESC, ts_rank DESC, ""Net"" DESC, ""InsurancePayment"" DESC
LIMIT {pageSize} OFFSET {offset};";
            }

            var drugs = await _context.Drugs
                .FromSqlInterpolated(sql)
                .AsNoTracking()
                .ToListAsync();

            // Safety net: if limited query returned zero (race/edge), fallback once to global (still enforcing ScriptCode)
            if (!useGlobal && drugs.Count == 0)
            {
                FormattableString fallbackSql = $@"
WITH ranked AS (
    SELECT d.*,
           CAST(0 AS numeric) AS ""Net"",
           CAST(0 AS numeric) AS ""InsurancePayment"",
           ROW_NUMBER() OVER (
               ORDER BY (
                   similarity(d.""name_unaccent"", unaccent({drugName})) * 0.5 +
                   ts_rank(d.""name_tsv"", plainto_tsquery(unaccent({drugName}))) * 0.3 +
                   CASE WHEN d.""name_soundex"" = soundex(unaccent({drugName})) THEN 0.1 ELSE 0 END +
                   CASE WHEN d.""name_unaccent"" ILIKE '%' || unaccent({drugName}) || '%' THEN 0.1 ELSE 0 END
               ) DESC,
               1
           ) AS rn,
           similarity(d.""name_unaccent"", unaccent({drugName})) AS sim,
           ts_rank(d.""name_tsv"", plainto_tsquery(unaccent({drugName}))) AS ts_rank
    FROM ""Drugs"" d
    WHERE
        (
            d.""name_unaccent"" % unaccent({drugName}) OR
            d.""name_tsv"" @@ plainto_tsquery(unaccent({drugName})) OR
            d.""name_soundex"" = soundex(unaccent({drugName})) OR
            d.""name_unaccent"" ILIKE '%' || unaccent({drugName}) || '%'
        )
        AND EXISTS (
            SELECT 1
            FROM ""DrugInsurances"" di
            WHERE di.""DrugId"" = d.""Id""
              AND di.""ScriptCode"" IS NOT NULL
              AND di.""ScriptCode"" <> ''
        )
)
SELECT *
FROM ranked
ORDER BY sim DESC, ts_rank DESC, ""Net"" DESC, ""InsurancePayment"" DESC
LIMIT {pageSize} OFFSET {offset};";

                drugs = await _context.Drugs
                    .FromSqlInterpolated(fallbackSql)
                    .AsNoTracking()
                    .ToListAsync();
            }

            return drugs;
        }

        internal async Task<ICollection<DrugModal>> GetDrugClassesByInsuranceNamePaginated(
            string insurance,
            string drugClassName,
            int pageSize,
            int pageNumber,
            string classType = "ClassV1"
        )
        {
            var query =
                from di in _context.DrugInsurances
                join drug in _context.Drugs on di.DrugId equals drug.Id
                join dc in _context.DrugClasses on drug.Id equals dc.DrugId
                join ci in _context.ClassInfos on dc.ClassId equals ci.Id
                join ct in _context.ClassTypes on ci.ClassTypeId equals ct.Id
                join ins in _context.InsuranceRxes on di.InsuranceId equals ins.Id
                where EF.Functions.ILike(ins.RxGroup, insurance)
                   && EF.Functions.ILike(ci.Name, $"%{drugClassName}%")
                   && EF.Functions.ILike(ct.Name, classType)
                orderby di.Net descending, di.InsurancePayment descending
                select new
                {
                    Drug = drug,
                    DrugInsurance = di,
                    ClassInfo = ci,
                    ClassType = ct
                };

            // Run SQL and materialize in memory
            var rawResults = await query.ToListAsync();

            // Group by ClassId and select the highest Net per class
            var groupedResults = rawResults
                .GroupBy(x => x.ClassInfo.Id)
                .Select(g => g.First())
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var result = groupedResults
                .Select(x => new DrugModal
                {
                    Id = x.Drug.Id,
                    Name = x.Drug.Name,
                    Ndc = x.Drug.NDC,
                    Form = x.Drug.Form,
                    Strength = x.Drug.Strength,
                    ClassId = x.ClassInfo.Id,
                    ClassType = x.ClassType.Name,
                    ClassName = x.ClassInfo.Name,
                    Acq = x.Drug.ACQ,
                    Awp = x.Drug.AWP,
                    Rxcui = x.Drug.Rxcui ?? 0,
                    Route = x.Drug.Route,
                    TeCode = x.Drug.TECode,
                    Ingrdient = x.Drug.Ingrdient,
                    ApplicationNumber = x.Drug.ApplicationNumber,
                    ApplicationType = x.Drug.ApplicationType,
                    StrengthUnit = x.Drug.StrengthUnit,
                    Type = x.Drug.Type
                })
                .ToList();

            return result;
        }

        internal async Task<ICollection<DrugModal>> GetDrugClassesByPCNPaginated(
            string insurance,
            string drugClassName,
            int pageSize,
            int pageNumber,
            string classType = "ClassV1")
        {
            pageNumber = pageNumber > 0 ? pageNumber : 1;
            pageSize = pageSize > 0 ? pageSize : 10;

            // Step 1: Query all matching rows ordered by Net + InsurancePayment
            var query =
                from di in _context.DrugInsurances
                join drug in _context.Drugs on di.DrugId equals drug.Id
                join dc in _context.DrugClasses on drug.Id equals dc.DrugId
                join ci in _context.ClassInfos on dc.ClassId equals ci.Id
                join ins in _context.InsuranceRxes on di.InsuranceId equals ins.Id
                join pcn in _context.InsurancePCNs on ins.InsurancePCNId equals pcn.Id
                join ct in _context.ClassTypes on ci.ClassTypeId equals ct.Id
                where EF.Functions.ILike(pcn.PCN, insurance)
                   && EF.Functions.ILike(ci.Name, $"%{drugClassName}%")
                   && EF.Functions.ILike(ct.Name, classType)
                orderby di.Net descending, di.InsurancePayment descending
                select new
                {
                    Drug = drug,
                    DrugInsurance = di,
                    ClassInfo = ci,
                    ClassType = ct
                };

            var rawResults = await query.ToListAsync();

            // Step 2: Distinct by ClassInfo.Id — in memory
            var distinctResults = rawResults
                .GroupBy(x => x.ClassInfo.Id)
                .Select(g => g.First()) // take best (highest Net) drug for each class
                .OrderBy(x => x.ClassInfo.Id) // stable ordering
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new DrugModal
                {
                    Id = x.Drug.Id,
                    Name = x.Drug.Name,
                    Ndc = x.Drug.NDC,
                    Form = x.Drug.Form,
                    Strength = x.Drug.Strength,
                    ClassId = x.ClassInfo.Id,
                    ClassType = x.ClassType.Name,
                    ClassName = x.ClassInfo.Name,
                    Acq = x.Drug.ACQ,
                    Awp = x.Drug.AWP,
                    Rxcui = x.Drug.Rxcui ?? 0,
                    Route = x.Drug.Route,
                    TeCode = x.Drug.TECode,
                    Ingrdient = x.Drug.Ingrdient,
                    ApplicationNumber = x.Drug.ApplicationNumber,
                    ApplicationType = x.Drug.ApplicationType,
                    StrengthUnit = x.Drug.StrengthUnit,
                    Type = x.Drug.Type
                })
                .ToList();

            return distinctResults;
        }

        internal async Task<ICollection<DrugModal>> GetDrugClassesByBINPaginated(
            string insurance,
            string drugClassName,
            int pageSize,
            int pageNumber,
            string classType = "ClassV1")
        {
            pageNumber = pageNumber > 0 ? pageNumber : 1;
            pageSize = pageSize > 0 ? pageSize : 10;

            // Step 1: query all matching rows, ordered
            var query =
                from di in _context.DrugInsurances
                join drug in _context.Drugs on di.DrugId equals drug.Id
                join dc in _context.DrugClasses on drug.Id equals dc.DrugId
                join ci in _context.ClassInfos on dc.ClassId equals ci.Id
                join ct in _context.ClassTypes on ci.ClassTypeId equals ct.Id
                join ins in _context.InsuranceRxes on di.InsuranceId equals ins.Id
                join pcn in _context.InsurancePCNs on ins.InsurancePCNId equals pcn.Id
                join insMain in _context.Insurances on pcn.InsuranceId equals insMain.Id
                where EF.Functions.ILike(insMain.Bin, insurance)
                   && EF.Functions.ILike(ci.Name, $"%{drugClassName}%")
                   && EF.Functions.ILike(ct.Name, classType)
                orderby di.Net descending, di.InsurancePayment descending
                select new
                {
                    Drug = drug,
                    DrugInsurance = di,
                    ClassInfo = ci,
                    ClassType = ct
                };

            var rawResults = await query.ToListAsync();

            // Step 2: distinct by ClassInfo.Id — in memory
            var distinctResults = rawResults
                .GroupBy(x => x.ClassInfo.Id)
                .Select(g => g.First()) // pick best drug for each class
                .OrderBy(x => x.ClassInfo.Id) // stable order
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new DrugModal
                {
                    Id = x.Drug.Id,
                    Name = x.Drug.Name,
                    Ndc = x.Drug.NDC,
                    Form = x.Drug.Form,
                    Strength = x.Drug.Strength,
                    ClassId = x.ClassInfo.Id,
                    ClassType = x.ClassType.Name,
                    ClassName = x.ClassInfo.Name,
                    Acq = x.Drug.ACQ,
                    Awp = x.Drug.AWP,
                    Rxcui = x.Drug.Rxcui ?? 0,
                    Route = x.Drug.Route,
                    TeCode = x.Drug.TECode,
                    Ingrdient = x.Drug.Ingrdient,
                    ApplicationNumber = x.Drug.ApplicationNumber,
                    ApplicationType = x.Drug.ApplicationType,
                    StrengthUnit = x.Drug.StrengthUnit,
                    Type = x.Drug.Type
                })
                .ToList();

            return distinctResults;
        }


        internal async Task<IEnumerable<Drug>> GetDrugsByClassId(int classId, string classType, int pageSize, int pageNumber)
        {
            pageNumber = pageNumber > 0 ? pageNumber : 1;
            pageSize = pageSize > 0 ? pageSize : 10;

            var query = from drug in _context.Drugs
                        join drugClass in _context.DrugClasses on drug.Id equals drugClass.DrugId
                        join classInfo in _context.ClassInfos on drugClass.ClassId equals classInfo.Id
                        join classTypeEntity in _context.ClassTypes on classInfo.ClassTypeId equals classTypeEntity.Id
                        where classInfo.Id == classId && classTypeEntity.Name == classType
                        select drug;

            var pagedResults = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return pagedResults;
        }
        internal async Task<int> ClearClassNames()
        {
            var classes = await _context.ClassInfos
                                         .Where(x => x.ClassTypeId == 2)
                                         .ToListAsync();

            string[] patternsToRemove = {
        @"\s?\(ONT\)",
        @"\s?\(CRE\)",
        @"\s?\(TAB\)",
        @"\s?\(CAP\)"
    };

            int cleanedCount = 0;

            foreach (var classInfo in classes)
            {
                string originalName = classInfo.Name;
                string cleanedName = originalName;

                foreach (var pattern in patternsToRemove)
                {
                    cleanedName = Regex.Replace(cleanedName, pattern, "", RegexOptions.IgnoreCase);
                }

                cleanedName = cleanedName.Trim();

                if (cleanedName != originalName)
                {
                    classInfo.Name = cleanedName;
                    cleanedCount++;
                }
            }

            await _context.SaveChangesAsync();

            return cleanedCount;
        }
        internal async Task<int> CleanAndMergeClasses()
        {
            var classes = await _context.ClassInfos
                                        .Where(x => x.ClassTypeId == 2)
                                        .ToListAsync();

            string[] patternsToRemove = {
        @"\s?\(ONT\)", @"\s?\(CRE\)", @"\s?\(TAB\)", @"\s?\(CAP\)"
    };

            // Step 1: Clean names
            var cleaned = classes.Select(c => new
            {
                Original = c,
                CleanedName = patternsToRemove.Aggregate(c.Name, (name, pattern) =>
                    Regex.Replace(name, pattern, "", RegexOptions.IgnoreCase)).Trim()
            });

            // Step 2: Group by cleaned name
            var grouped = cleaned.GroupBy(x => x.CleanedName);

            int totalMerged = 0;

            foreach (var group in grouped)
            {
                var items = group.Select(x => x.Original).ToList();

                if (items.Count <= 1)
                {
                    // No merge needed, just rename
                    items[0].Name = group.Key;
                    continue;
                }

                var master = items.OrderBy(x => x.Id).First();
                master.Name = group.Key;

                var duplicates = items.Where(x => x.Id != master.Id).ToList();

                foreach (var dup in duplicates)
                {
                    // Update all DrugClass pointing to duplicate -> master
                    var links = await _context.DrugClasses
                                              .Where(dc => dc.ClassId == dup.Id)
                                              .ToListAsync();

                    foreach (var link in links)
                    {
                        // Check if a link with master.Id already exists
                        bool alreadyExists = await _context.DrugClasses
                            .AnyAsync(dc => dc.DrugId == link.DrugId && dc.ClassId == master.Id);

                        if (!alreadyExists)
                        {
                            // Create a new entry pointing to master class
                            _context.DrugClasses.Add(new DrugClass
                            {
                                DrugId = link.DrugId,
                                ClassId = master.Id
                            });
                        }

                        // Always remove the old one pointing to the duplicate class
                        _context.DrugClasses.Remove(link);
                    }

                    // Remove the duplicate class entry
                    _context.ClassInfos.Remove(dup);
                    totalMerged++;
                }
            }

            await _context.SaveChangesAsync();
            return totalMerged;
        }
        internal async Task<ICollection<string>> GetAllDrugClassesVersions()
        {
            var items = await _context.ClassTypes.Select(x => x.Name).Where(x => !x.Contains("test") && !x.Contains("Test")).ToListAsync();
            return items;
        }
        internal async Task<ICollection<SuperAdminDrugReadDto>> GetAllDrugsForSuperAdminAsync(
            int pageNumber, int pageSize)
        {
            pageNumber = pageNumber > 0 ? pageNumber : 1;
            pageSize = pageSize > 0 ? pageSize : 10;

            var drugs = await _context.Drugs
                .OrderBy(d => d.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new SuperAdminDrugReadDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    NDC = d.NDC,
                    Form = d.Form,
                    Strength = d.Strength,
                    ACQ = d.ACQ,
                    AWP = d.AWP,
                    Rxcui = d.Rxcui ?? 0,
                    Route = d.Route,
                    TECode = d.TECode,
                    Ingrdient = d.Ingrdient,
                    ApplicationNumber = d.ApplicationNumber,
                    ApplicationType = d.ApplicationType,
                    StrengthUnit = d.StrengthUnit,
                    Type = d.Type,

                    Classes = _context.DrugClasses
                        .Where(dc => dc.DrugId == d.Id)
                        .Select(dc => dc.ClassInfo)
                        .ToList(),

                    RxGroups = _context.DrugInsurances
                        .Where(di => di.DrugId == d.Id)
                        .Select(di => di.Insurance)
                        .Distinct()
                        .ToList()
                })
                .ToListAsync();

            return drugs;
        }

    }
    // public sealed class InsuranceMap : ClassMap<Insurance>
    // {
    //     public InsuranceMap()
    //     {
    //         Map(m => m.Bin).Name("PCN");
    //         Map(m => m.Pcn).Name("Bin");
    //         Map(m => m.Name).Name("InsuranceShortName");
    //         Map(m => m.RxGroup).Name("RxGroup");
    //     }
    // }

}