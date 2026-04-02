using AutoMapper;
using SearchTool_ServerSide.Dtos.DiseaseDtos;
using SearchTool_ServerSide.Models;

namespace SearchTool_ServerSide.Profiles
{
    public class DiseaseProfile : Profile
    {
        public DiseaseProfile()
        {
            CreateMap<Disease, DiseaseReadDto>().ReverseMap();
            CreateMap<DiseaseAddDto, Disease>().ReverseMap();


            CreateMap<DrugDiseaseHistoryAddDto, DrugDiseaseAddHistory>().ReverseMap();
            CreateMap<DrugDiseaseAddHistory, DrugDiseaseHistoryReadDto>().ReverseMap();
        }
    }
}