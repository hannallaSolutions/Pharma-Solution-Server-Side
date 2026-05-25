using System.Diagnostics;
using System.Drawing;
using AutoMapper;
using SearchTool_ServerSide.Dtos;
using SearchTool_ServerSide.Dtos.ClassDtos;
using SearchTool_ServerSide.Dtos.DrugDtos;
using SearchTool_ServerSide.Dtos.InsuranceDtos.cs;
using SearchTool_ServerSide.Dtos.ScritpsDto;
using SearchTool_ServerSide.Models;
using SearchTool_ServerSide.Repository;
using ServerSide.Models;
using static SearchTool_ServerSide.Repository.DrugRepository;

namespace SearchTool_ServerSide.Services
{
    public class DrugService(DrugRepository _drugRepository, IMapper _mapper)
    {
        public async Task Procces()
        {
            await _drugRepository.SaveData();
        }
        public async Task Procces2()
        {
            await _drugRepository.ImportDrugInsuranceAsync();
        }
        public async Task<ICollection<Drug>> SearchName(string name, int pageNumber, int pageSize,bool isDemo=false)
        {
            var items = await _drugRepository.GetDrugsByName(name, pageNumber, pageSize,isDemo);
            return items;
        }
        public async Task<int> ImportDrugInsuranceFileAsync(IFormFile uploadedFile, CancellationToken ct = default)
        {
            var result = await _drugRepository.ImportDrugInsuranceFileAsync(uploadedFile, ct);
            return result;
        }
        public async Task<ICollection<DrugModal>> GetClassesByName(string name, string classVersion, int pageNumber, int pageSize)
        {
            var items = await _drugRepository.GetClassesByName(name, classVersion, pageNumber, pageSize);
            return items;
        }

        // public async Task<ICollection<Insurance>> GetDrugInsurances(string name)
        // {
        //     var items = await _drugRepository.GetDrugInsurances(name);
        //     return items;
        // }

        public async Task<ICollection<string>> GetDrugNDCs(string name)
        {
            Console.WriteLine("here : ");
            var items = await _drugRepository.GetAllNDCByDrugName(name);
            return items;
        }

        public async Task<DrugInsurance> GetBySelection(string name, string ndc, string insuranceName)
        {
            var item = await _drugRepository.GetBySelection(name, ndc, insuranceName);
            return item;
        }





        internal async Task<Drug> SearchByIdNdc(int id, string ndc)
        {
            var item = await _drugRepository.SearchByIdNdc(id, ndc);
            return item;
        }

        internal async Task<Drug> SearchByNdc(string ndc)
        {
            var item = await _drugRepository.GetDrugByNdc(ndc);
            return item;
        }

        internal async Task<DrugsAlternativesReadDto> GetDetails(string ndc, int sourceInsuranceId, int? insuranceId,int branchId,int userId)
        {
            var item = await _drugRepository.GetDetails(ndc, sourceInsuranceId, insuranceId,branchId,userId);
            return item;
        }

        // internal async Task<ICollection<string>> getDrugNDCsByNameInsuance(string drugName, int insurnaceId)
        // {
        //     var items = await _drugRepository.getDrugNDCsByNameInsuance(drugName, insurnaceId);
        //     return items;
        // }

        internal async Task<DrugClass> getClassbyId(int id)
        {
            var item = await _drugRepository.getClassbyId(id);
            return item;
        }
        internal async Task<IEnumerable<ClassInfoReadDto>> GetClassesByDrugId(int drugId)
        {
            return await _drugRepository.GetClassesByDrugId(drugId);
        }
        internal async Task<ICollection<Drug>> GetDrugsByClass(int classId)
        {
            var items = await _drugRepository.GetDrugsByClass(classId);
            return items;
        }

        internal async Task<ICollection<DrugInsurance>> GetAllLatest()
        {
            var items = await _drugRepository.GetAllLatest();
            return items;
        }

        internal async Task<ICollection<AuditReadDto>> GetAllLatestScriptsPaginated(int page = 1, int pageSize = 1000, string classVersion = "ClassV1", string matchOn = "BIN",int mainCompanyId=1,int BranchId=1)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var items = await _drugRepository.GetAllLatestScriptsPaginated(page, pageSize, classVersion, matchOn, mainCompanyId, BranchId);
            stopwatch.Stop();
            Console.WriteLine("Total time taken : " + stopwatch.ElapsedMilliseconds);
            return items;
        }

        internal async Task<ICollection<DrugsAlternativesReadDto>> GetAllDrugs(int classId, string sourceDrugNDC, int pageNumber, int pageSize)
        {
            var items = await _drugRepository.GetAllDrugs(classId, sourceDrugNDC, pageNumber, pageSize);
            return items;
        }


        internal async Task<Drug> GetDrugById(int id)
        {
            var item = await _drugRepository.GetDrugById(id);
            return item;
        }

        // internal async Task oneway()
        // {
        //     await _drugRepository.oneway();
        // }

        internal async Task<ICollection<DrugInsuranceReadDto>> GetInsuranceByNdc(string ndc)
        {
            var items = await _drugRepository.GetInsuranceByNdc(ndc);
            return items;

        }

        internal async Task<ICollection<ScriptItemDto>> GetScriptByScriptCode(string scriptCode)
        {
            var items = await _drugRepository.GetScriptByScriptCode(scriptCode);
            return items;
        }
        internal async Task ImportInsurancesFromCsvAsync()
        {
            await _drugRepository.ImportInsurancesFromCsvAsync();
        }


        internal async Task<ICollection<Drug>> GetDrugsByClassBranch(int classId, int branchId)
        {
            var items = await _drugRepository.GetDrugsByClassBranch(classId, branchId);
            return items;
        }
        internal async Task<Script> GetScriptAsync(string scriptCode)
        {
            var item = await _drugRepository.GetScriptAsync(scriptCode);
            return item;
        }


        internal async Task<ICollection<Drug>> GetDrugsByInsurance(int insuranceId, string drug)
        {
            var items = await _drugRepository.GetDrugsByInsurance(insuranceId, drug);
            return items;
        }
        internal async Task<ICollection<Drug>> GetDrugsByInsurance(string insruance)
        {
            var items = await _drugRepository.GetDrugsByInsurance(insruance);
            return items;
        }
        internal async Task<ICollection<Drug>> GetDrugsByInsuranceNameDrugName(string insuranceName, string drugName, int pageSize = 1000, int pageNumber = 1)
        {
            var items = await _drugRepository.GetDrugsByInsuranceNameDrugName(insuranceName, drugName, pageSize, pageNumber);
            return items;
        }
        internal async Task<ICollection<Drug>> GetDrugsByPCN(string pcn)
        {
            var items = await _drugRepository.GetDrugsByPCN(pcn);
            return items;
        }
        internal async Task<ICollection<Drug>> GetDrugsByBIN(string bin)
        {
            var items = await _drugRepository.GetDrugsByBIN(bin);
            return items;
        }
        internal async Task<ICollection<Insurance>> GetInsurances(string insruance)
        {
            var items = await _drugRepository.GetInsurances(insruance);
            return items;
        }

        internal async Task<ICollection<Insurance>> GetInsurancesBinsByName(string bin)
        {
            var items = await _drugRepository.GetInsurancesBinsByName(bin);
            return items;
        }
        internal async Task<ICollection<InsurancePCN>> GetInsurancesPcnByBinId(int binId)
        {
            var items = await _drugRepository.GetInsurancesPcnByBinId(binId);
            return items;
        }
        internal async Task<ICollection<InsuranceRx>> GetInsurancesRxByPcnId(int pcnId)
        {
            var items = await _drugRepository.GetInsurancesRxByPcnId(pcnId);
            return items;
        }



        internal async Task<IEnumerable<Drug>> GetAll()
        {
            var items = await _drugRepository.GetAll();
            return items;
        }
        internal async Task<IEnumerable<Drug>> GetAllV2()
        {
            var items = await _drugRepository.GetAllV2();
            return items;
        }


        // internal async Task<DrugBestAlternativeReadDto> GetBestAlternativeByNDCRxGroupId(int classId, int rxGroupId)
        // {
        //     var items = await _drugRepository.GetBestAlternativeByNDCRxGroupId(classId, rxGroupId);
        //     return items;
        // }
        internal async Task AddMediCare()
        {
            await _drugRepository.AddMediCare();
        }

        internal async Task<ICollection<DrugMediReadDto>> GetAllMediDrugs(int classId)
        {
            var items = await _drugRepository.GetAllMediDrugs(classId);
            return items;
        }

        internal async Task<ICollection<Drug>> GetDrugsByInsuranceNamePaginated(string insurance, string drugName, int pageSize, int pageNumber, bool isDemo = false,int branchId =1)
        {
            return await _drugRepository.GetDrugsByInsuranceNamePaginated(insurance, drugName, pageSize, pageNumber,isDemo,branchId);
        }
        internal async Task<ICollection<Drug>> GetDrugsByPCNPaginated(string insurance, string drugName, int pageSize, int pageNumber, bool isDemo = false, int branchId = 1)
        {
            return await _drugRepository.GetDrugsByPCNPaginated(insurance, drugName, pageSize, pageNumber,isDemo, branchId);
        }
        internal async Task<ICollection<Drug>> GetDrugsByBINPaginated(string insurance, string drugName, int pageSize, int pageNumber, bool isDemo = false, int branchId = 1)
        {
            return await _drugRepository.GetDrugsByBINPaginated(insurance, drugName, pageSize, pageNumber, isDemo, branchId);
        }

        internal async Task<ICollection<DrugModal>> GetDrugClassesByInsuranceNamePagintated(string insurance, string drugClassName, int pageSize, int pageNumber, string ClassVersion = "ClassV1")
        {
            var items = await _drugRepository.GetDrugClassesByInsuranceNamePaginated(insurance, drugClassName, pageSize, pageNumber, ClassVersion);
            return items;
        }

        internal async Task<ICollection<DrugModal>> GetDrugClassesByPCNPagintated(string insurance, string drugClassName, int pageSize, int pageNumber, string ClassVersion = "ClassV1")
        {
            var items = await _drugRepository.GetDrugClassesByPCNPaginated(insurance, drugClassName, pageSize, pageNumber, ClassVersion);
            return items;
        }

        internal async Task<ICollection<DrugModal>> GetDrugClassesByBINPagintated(string insurance, string drugClassName, int pageSize, int pageNumber, string ClassVersion = "ClassV1")
        {
            var items = await _drugRepository.GetDrugClassesByBINPaginated(insurance, drugClassName, pageSize, pageNumber, ClassVersion);
            return items;
        }

        internal async Task<IEnumerable<Drug>> GetDrugsByClassId(int classId, string ClassType, int pageSize, int pageNumber)
        {
            return await _drugRepository.GetDrugsByClassId(classId, ClassType, pageSize, pageNumber);
        }
        internal async Task<int> ClearClassNames()
        {
            return await _drugRepository.ClearClassNames();
        }
        internal async Task<int> AddScripts(ICollection<ScriptAddDto> scriptAddDtos)
        {
            return await _drugRepository.AddScripts(scriptAddDtos);
        }
        internal async Task<int> CleanAndMergeClasses()
        {
            return await _drugRepository.CleanAndMergeClasses();
        }
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
        string? diseaseName = null,
        int branchId = 1, bool isDemo = false,int userId=0)
        {
            return await _drugRepository.GetAlternativesWithInsurance(classInfoId, 
            sourceDrugNDC, sourceRxGroupId, matchedRx, pageNumber, pageSize, rxgroup, pcn, bin, diseaseName, branchId,isDemo,userId);
        }
        internal async Task<AlternativesFilterOptionsDto> GetAlternativesWithInsuranceFilters(int classInfoId, string sourceDrugNDC, string? rxgroup = null, string? pcn = null, string? bin = null)
        {
            return await _drugRepository.GetAlternativesWithInsuranceFilters(classInfoId, sourceDrugNDC, rxgroup, pcn, bin);
        }
        public async Task<PagedResult<DrugAlternativeLiteDto>> GetAlternativesNoInsurancePaged(
            int classInfoId,
            int rxgroupId,
            string sourceDrugNDC,
            int pageNumber = 1,
            int pageSize = 10, int branchId = 0,bool isDemo = false)
        {
            return await _drugRepository.GetAlternativesNoInsurancePaged(classInfoId, rxgroupId, sourceDrugNDC, pageNumber, pageSize, branchId,isDemo);
        }
        internal async Task<(ICollection<string> InsuranceNDC, ICollection<string> AllInsuranceNDC)> GetDrugInsuranceNDCS(string drugName, int? insuranceId)
        {
            var res = await _drugRepository.GetDrugInsuranceNDCS(drugName, insuranceId);
            return res;
        }
        internal async Task<ICollection<string>> GetAllDrugClassesVersions()
        {
            var items = await _drugRepository.GetAllDrugClassesVersions();
            return items;
        }
        internal async Task<int> AddClassVersion(IFormFile uploadedFile, ClassTypeAddDto classTypeAddDto, bool isMultiple = false, CancellationToken ct = default)
        {
            var result = await _drugRepository.AddClassVersion(uploadedFile, classTypeAddDto, isMultiple, ct);
            return result;
        }
        internal async Task<ICollection<SuperAdminDrugReadDto>> GetAllDrugsForSuperAdminAsync(int pageNumber = 1, int pageSize = 20)
        {
            var items = await _drugRepository.GetAllDrugsForSuperAdminAsync(pageNumber, pageSize);
            return items;
        }
    }
}