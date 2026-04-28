using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SearchTool_ServerSide.Data;
using SearchTool_ServerSide.Dtos.BranchDTOs;
using SearchTool_ServerSide.Models;

namespace SearchTool_ServerSide.Repository
{
    public class BranchRepository : GenericRepository<Branch>
    {
        private readonly SearchToolDBContext _context;  //to read from database
        private readonly IMapper _mapper; // to map between entities and DTOs
        public BranchRepository(SearchToolDBContext context, IMapper mapper) : base(context)
        {
            _context = context;
            _mapper = mapper;
        }

        internal async Task<MainCompany> GetMainCompanyByBranchId(int branchId)
        {
            var branch = await _context.Branches
                .Include(b => b.MainCompany)
                .Include(b => b.MainCompany.ClassType)
                .FirstOrDefaultAsync(b => b.Id == branchId);
            return branch?.MainCompany;
        }
        internal async Task<ICollection<Branch>> GetAllBranchesByMainCompanyId(int mainCompanyId)
        {
            return await _context.Branches
                .Where(b => b.MainCompanyId == mainCompanyId)
                .ToListAsync();
        }

        internal async Task<ICollection<Branch>> GetAllBranches()
        {
            return await _context.Branches.ToListAsync();
        }

        //createasync with createbeanchdto


       /* internal async Task<Branch> CreateAsync(CreateBranchDto branch)
        {
            var branchEntity = _mapper.Map<Branch>(branch);
            await _context.Branches.AddAsync(branchEntity);
            await _context.SaveChangesAsync();
            return branchEntity;
            
            }
       */
       internal async Task<Branch> CreateAsync(CreateBranchDto dto)
{
    var branchEntity = new Branch
    {
        Name = dto.Name,
        Location = dto.Location,
        Code = dto.Code,
        MainCompanyId = dto.MainCompanyId
    };

    await _context.Branches.AddAsync(branchEntity);
    await _context.SaveChangesAsync();
    return branchEntity;
}

    //get branch by id
    internal async Task<Branch> GetBranchById(int branchId)
    {
        return await _context.Branches.FindAsync(branchId);
    }

    //delete branch by id
    internal async Task DeleteAsync(Branch branch)
    {
        _context.Branches.Remove(branch);
        await _context.SaveChangesAsync();  

    }

        internal async Task<Branch> GetByIdAsync(int branchId)
        {
            throw new NotImplementedException();
        }

        //update branch
        internal async Task UpdateAsync(Branch branch)
        {
            _context.Branches.Update(branch);
            await _context.SaveChangesAsync();
        }

 
        internal async Task<bool> EditBranch(int branchId, EditBranchDto dto)
        {
            var branch = await _context.Branches.FindAsync(branchId);
            if (branch == null) return false;

            branch.Name = dto.Name;
            branch.Location = dto.Location;
            branch.Code = dto.Code;

            // if you want to allow changing the main company association, update it as well
            branch.MainCompanyId = dto.MainCompanyId;

            await _context.SaveChangesAsync();
            return true;
    }

// delete branch by id
        internal async Task<bool> DeleteBranchById(int branchId)
        {
            var branch = await _context.Branches.FindAsync(branchId);
            if (branch == null) return false;

            _context.Branches.Remove(branch);
            await _context.SaveChangesAsync();
            return true;
    }

    }
}