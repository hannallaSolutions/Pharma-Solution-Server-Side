using AutoMapper;
using SearchTool_ServerSide.Models;
using SearchTool_ServerSide.Repository;

namespace SearchTool_ServerSide.Services
{
    public class BranchService(BranchRepository _branchRepository, IMapper _mapper)
    {
        public async Task<MainCompany> GetMainCompanyByBranchId(int branchId)
        {
            return await _branchRepository.GetMainCompanyByBranchId(branchId);

        }
        public async Task<ICollection<Branch>> GetAllBranchesByMainCompanyId(int mainCompanyId)
        {
            return await _branchRepository.GetAllBranchesByMainCompanyId(mainCompanyId);
        }
    }
}