using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SearchTool_ServerSide.Data;
using SearchTool_ServerSide.Dtos.DiseaseDtos;
using SearchTool_ServerSide.Dtos.DrugDtos;
using SearchTool_ServerSide.Repository;

namespace SearchTool_ServerSide.Models
{
    public class DiseaseRepository : GenericRepository<Disease>
    {
        private readonly SearchToolDBContext _context;
        private readonly IMapper _mapper;
        public DiseaseRepository(SearchToolDBContext context, IMapper mapper) : base(context)
        {
            _context = context;
            _mapper = mapper;
        }
        internal async Task<DiseaseReadDto?> AddDisease(DiseaseAddDto diseaseAddDto)
        {
            var diseaseEntity = _mapper.Map<Disease>(diseaseAddDto);
            await _context.Diseases.AddAsync(diseaseEntity);
            await _context.SaveChangesAsync();
            return _mapper.Map<DiseaseReadDto>(diseaseEntity);
        }
        internal async Task<IEnumerable<DiseaseReadDto>> GetAllDiseasesAsync()
        {
            var diseases = await GetAll();
            return _mapper.Map<IEnumerable<DiseaseReadDto>>(diseases);
        }
        internal async Task<Disease?> GetDiseaseByName(string name)
        {
            var disease = await _context.Diseases.FirstOrDefaultAsync(d => d.Name == name && d.Show);
            if (disease == null)
            {
                return null;
            }
            return disease;
        }
        internal async Task<IEnumerable<Disease?>> SearchByDisease(string name)
        {
            var diseases = await _context.Diseases
                .Where(d => d.Name.ToLower().Contains(name.ToLower()) && d.Show)
                .ToListAsync();
            return diseases;
        }
        internal async Task<bool> SoftDeleteDisease(int id)
        {
            var disease = await _context.Diseases.FirstOrDefaultAsync(d => d.Id == id && d.Show);
            if (disease == null)
            {
                return false;
            }
            disease.Show = false;
            _context.Diseases.Update(disease);
            await _context.SaveChangesAsync();
            return true;
        }
        internal async Task<bool> DiseaseExists(string name)
        {
            return await _context.Diseases.AnyAsync(d => d.Name == name && d.Show);
        }
        internal async Task<DrugDiseaseHistoryReadDto>? AddDrugDisease(DrugDiseaseHistoryAddDto drugDiseaseHistoryAddDto)
        {
            var drugDiseaseHistoryEntity = _mapper.Map<DrugDiseaseAddHistory>(drugDiseaseHistoryAddDto);
            await _context.DrugDiseaseAddHistories.AddAsync(drugDiseaseHistoryEntity);
            await _context.SaveChangesAsync();
            var drugDiseaseHistoryReadDto = _mapper.Map<DrugDiseaseHistoryReadDto>(drugDiseaseHistoryEntity);
            drugDiseaseHistoryReadDto.UserEmail = drugDiseaseHistoryEntity.User.Email;
            drugDiseaseHistoryReadDto.DrugName = drugDiseaseHistoryEntity.Drug.Name;
            drugDiseaseHistoryReadDto.DiseaseName = drugDiseaseHistoryEntity.Disease.Name;
            drugDiseaseHistoryReadDto.UserName = drugDiseaseHistoryEntity.User.Name;
            return drugDiseaseHistoryReadDto;
        }
        internal async Task<IEnumerable<DrugDiseaseHistoryReadDto>> GetAllDrugDiseasesByDrugIdAsync(int id)
        {
            var drugDiseaseHistories = await _context.DrugDiseaseAddHistories
                .Include(dd => dd.Drug)
                .Include(dd => dd.Disease)
                .Where(dd => dd.Show && dd.DrugId == id)
                .GroupBy(dd => dd.DiseaseId)
                .Select(g => g.First())
                .ToListAsync();
            return _mapper.Map<IEnumerable<DrugDiseaseHistoryReadDto>>(drugDiseaseHistories);
        }
        internal async Task<DrugDiseaseHistoryReadDto>? AddDrugDiseaseHistory(DrugDiseaseHistoryAddDto drugDiseaseHistoryAddDto)
        {
            var drugDisease = await _context.DrugDiseaseAddHistories
                .FirstOrDefaultAsync(dd => dd.DrugId == drugDiseaseHistoryAddDto.DrugId && dd.DiseaseId == drugDiseaseHistoryAddDto.DiseaseId && dd.UserId == drugDiseaseHistoryAddDto.UserId && dd.Show);
            var drugDiseaseHistoryEntity = new DrugDiseaseAddHistory();

            if (drugDisease == null)
            {
                drugDiseaseHistoryEntity = new DrugDiseaseAddHistory
                {
                    DrugId = drugDiseaseHistoryAddDto.DrugId,
                    DiseaseId = drugDiseaseHistoryAddDto.DiseaseId,
                    UserId = drugDiseaseHistoryAddDto.UserId,
                    CreatedAt = DateTime.UtcNow,
                    EditedAt = DateTime.UtcNow,
                    Show = true
                };
                await _context.DrugDiseaseAddHistories.AddAsync(drugDiseaseHistoryEntity);
                drugDisease = drugDiseaseHistoryEntity;

            }
            else
            {

                drugDisease.EditedAt = DateTime.UtcNow;
                drugDisease.Show = true;

                _context.DrugDiseaseAddHistories.Update(drugDisease);
            }

            await _context.SaveChangesAsync();
            var item = await _context.DrugDiseaseAddHistories
                .Include(dd => dd.Drug)
                .Include(dd => dd.Disease)
                .Include(dd => dd.User)
                .FirstOrDefaultAsync(dd => dd.Id == drugDisease.Id);
            var dto = _mapper.Map<DrugDiseaseHistoryReadDto>(item);
            dto.UserEmail = item.User.Email;
            dto.UserName = item.User.Name;
            dto.DrugName = item.Drug.Name;
            dto.DiseaseName = item.Disease.Name;
            return dto;
        }
        internal async Task<IEnumerable<DrugReadDto>> GetDrugInteractions(string diseaseName, int pageSize = 10, int pageNumber = 1)
        {
            var disease = await _context.Diseases
                .FirstOrDefaultAsync(d => d.Name == diseaseName && d.Show);
            if (disease == null)
            {
                return new List<DrugReadDto>();
            }

            var interactingDiseases = await _context.DrugDiseaseAddHistories
                .Include(dd => dd.Drug)
                .Where(dd => dd.Show && dd.DiseaseId == disease.Id)
                .Select(dd => dd.Drug)
                .Distinct()
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return _mapper.Map<IEnumerable<DrugReadDto>>(interactingDiseases);
        }
        internal async Task<IEnumerable<DrugDiseaseHistoryReadDto>> GetDrugDiseaseHistory(string userEmail)
        {
            var historyEntries = await _context.DrugDiseaseAddHistories
                .Include(h => h.Drug)
                .Include(h => h.Disease)
                .Include(h => h.User)
                .Where(h => h.User.Email == userEmail && h.Show)
                .ToListAsync();

            return _mapper.Map<IEnumerable<DrugDiseaseHistoryReadDto>>(historyEntries);
        }
        internal async Task<bool> SoftDeleteDrugDiseaseHistory(int id)
        {
            var historyEntry = await _context.DrugDiseaseAddHistories
                .FirstOrDefaultAsync(h => h.Id == id && h.Show);
            if (historyEntry == null)
            {
                return false;
            }

            historyEntry.Show = false;
            _context.DrugDiseaseAddHistories.Update(historyEntry);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}