
        using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SearchTool_ServerSide.Data;
using SearchTool_ServerSide.Dtos.DiseaseDtos;
using SearchTool_ServerSide.Dtos.DrugDtos;
using SearchTool_ServerSide.Models;
using SearchTool_ServerSide.Models.Enums;

namespace SearchTool_ServerSide.Services
{
    public class DiseaseService
    {
        private readonly DiseaseRepository _diseaseRepository;
        private readonly SearchToolDBContext _db;
        private readonly IMapper _mapper;

        public DiseaseService(DiseaseRepository diseaseRepository, SearchToolDBContext db, IMapper mapper)
        {
            _diseaseRepository = diseaseRepository;
            _db = db;
            _mapper = mapper;
        }

        // باقي الميثودز...
    

        public async Task<IEnumerable<DiseaseReadDto>> GetAllDiseases()
        {
            return await _diseaseRepository.GetAllDiseasesAsync();
        }
        public async Task<Disease>? GetDiseaseByName(string name)
        {
            return await _diseaseRepository.GetDiseaseByName(name);
        }
        internal async Task<IEnumerable<Disease?>> SearchByDisease(string name)
        {
            var disease = await _diseaseRepository.SearchByDisease(name);

            return disease;
        }
                public async Task<DiseaseReadDto?> AddDisease(DiseaseAddDto diseaseAddDto)
        {
            return await _diseaseRepository.AddDisease(diseaseAddDto);
        }
        public async Task<bool> SoftDeleteDisease(int id)
        {
            return await _diseaseRepository.SoftDeleteDisease(id);
        }
        public async Task<bool> DiseaseExists(string name)
        {
            return await _diseaseRepository.DiseaseExists(name);
        }
        public async Task<DrugDiseaseHistoryReadDto>? AddDrugDisease(DrugDiseaseHistoryAddDto drugDiseaseAddDto)
        {
            var item = await _diseaseRepository.AddDrugDisease(drugDiseaseAddDto);

            return item;
        }
        public async Task<IEnumerable<DrugDiseaseHistoryReadDto>> GetAllDrugDiseasesByDrugIdAsync(int id)
        {
            return await _diseaseRepository.GetAllDrugDiseasesByDrugIdAsync(id);
        }
        public async Task<DrugDiseaseHistoryReadDto>? AddDrugDiseaseHistory(DrugDiseaseHistoryAddDto drugDiseaseHistoryAddDto)
        {

            return await _diseaseRepository.AddDrugDiseaseHistory(drugDiseaseHistoryAddDto);
        }
        public async Task<IEnumerable<DrugReadDto>> GetDrugInteractions(string diseaseName, int pageSize = 10, int pageNumber = 1)
        {
            return await _diseaseRepository.GetDrugInteractions(diseaseName, pageSize, pageNumber);
        }
        public async Task<IEnumerable<DrugDiseaseHistoryReadDto>> GetDrugDiseaseHistory(string userEmail)
        {
            return await _diseaseRepository.GetDrugDiseaseHistory(userEmail);
        }
        public async Task<bool> SoftDeleteDrugDiseaseHistory(int id)
        {
            return await _diseaseRepository.SoftDeleteDrugDiseaseHistory(id);
        }

        public async Task<IEnumerable<DiseaseReadDto>> GetVisibleDiseasesAsync(int userId)
{
    var settings = await _db.DiseaseVisibilitySettings.FirstOrDefaultAsync(x => x.Id == 1);
    var mode = settings?.Mode ?? DiseaseVisibilityMode.AllDoctors;

    // Base: only enabled diseases
    var q = _db.Diseases.AsNoTracking().Where(d => d.Show);

    if (mode == DiseaseVisibilityMode.AllDoctors)
    {
        var list = await q.OrderBy(d => d.Name).ToListAsync();
        return _mapper.Map<IEnumerable<DiseaseReadDto>>(list);
    }

    if (mode == DiseaseVisibilityMode.CustomizeByUser)
    {
        var allowedIds = _db.UserDiseaseVisibility
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.DiseaseId);

        var list = await q
            .Where(d => allowedIds.Contains(d.Id))
            .OrderBy(d => d.Name)
            .ToListAsync();

        return _mapper.Map<IEnumerable<DiseaseReadDto>>(list);
    }

    // OwnOnly later
    var fallback = await q.OrderBy(d => d.Name).ToListAsync();
    return _mapper.Map<IEnumerable<DiseaseReadDto>>(fallback);
}

    }
}