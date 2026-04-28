using AutoMapper;
using SearchTool_ServerSide.Dtos.BranchDTOs;
using SearchTool_ServerSide.Models;

namespace SearchTool_ServerSide.Profiles
{
    public class BranchProfile : Profile
    {
        public BranchProfile()
        {
            CreateMap<Branch, BranchDto>()
                .ForMember(dest => dest.MainCompanyName, opt => opt.MapFrom(src => src.MainCompany.Name))
                .ReverseMap();

            CreateMap<Branch, CreateBranchDto>().ReverseMap();
            CreateMap<Branch, EditBranchDto>().ReverseMap();
        }
    }
}