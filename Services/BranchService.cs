using AutoMapper;
using SearchTool_ServerSide.Dtos.BranchDTOs;
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

        public async Task<ICollection<Branch>> GetAllBranches()
        {
            return await _branchRepository.GetAllBranches();
        }

        //createasync
        public async Task<Branch> CreateAsync(CreateBranchDto branch)
        {
            return  await _branchRepository.CreateAsync(branch);
        }

    

        //edit branch
        public async Task<bool> EditBranch(int branchId, EditBranchDto branch)
        {
            var existingBranch = await _branchRepository.GetBranchById(branchId);
            if (existingBranch == null)
            {
                return false;
            }

            _mapper.Map(branch, existingBranch);
            await _branchRepository.UpdateAsync(existingBranch);
            return true;
        }

       //delete branch by id
        public async Task<bool> DeleteBranchById(int branchId)
        {
            var branch = await _branchRepository.GetBranchById(branchId);
            if (branch == null)
            {
                return false;
            }

            await _branchRepository.DeleteAsync(branch);
            return true;
        }

    }
}