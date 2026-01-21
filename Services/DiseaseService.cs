using AutoMapper;
using SearchTool_ServerSide.Dtos.DiseaseDtos;
using SearchTool_ServerSide.Dtos.DrugDtos;
using SearchTool_ServerSide.Models;

namespace SearchTool_ServerSide.Services
{
    public class DiseaseService(DiseaseRepository _diseaseRepository, IMapper mapper)
    {
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
    }
}