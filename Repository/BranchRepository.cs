using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SearchTool_ServerSide.Data;
using SearchTool_ServerSide.Dtos.BranchDTOs;
using SearchTool_ServerSide.Models;

namespace SearchTool_ServerSide.Repository
{
    public class BranchRepository : GenericRepository<Branch>
    {
        private readonly SearchToolDBContext _context;
        private readonly IMapper _mapper;

        public BranchRepository(SearchToolDBContext context, IMapper mapper) : base(context)
        {
            _context = context;
            _mapper = mapper;
        }

        internal async Task<MainCompany?> GetMainCompanyByBranchId(int branchId)
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


        internal async Task<ICollection<Branch>> GetAllMainCompanyBranchesByBranchId(int branchId)
        {
            var branch = await _context.Branches
                .FirstOrDefaultAsync(x => x.Id == branchId);

            if (branch == null)
            {
                return new List<Branch>();
            }

            return await _context.Branches
                .Where(x => x.MainCompanyId == branch.MainCompanyId)
                .ToListAsync();
        }

        internal async Task<ICollection<Branch>> GetAllBranches()
        {
            return await _context.Branches.ToListAsync();
        }

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

        internal async Task<Branch?> GetBranchById(int branchId)
        {
            return await _context.Branches.FindAsync(branchId);
        }

        internal async Task DeleteAsync(Branch branch)
        {
            _context.Branches.Remove(branch);
            await _context.SaveChangesAsync();
        }

        internal async Task UpdateAsync(Branch branch)
        {
            _context.Branches.Update(branch);
            await _context.SaveChangesAsync();
        }

        internal async Task<List<BranchWithUsersCountDto>> GetBranchesWithUsersCount()
        {
            return await _context.Branches
                .Select(b => new BranchWithUsersCountDto
                {
                    BranchId = b.Id,
                    BranchName = b.Name,
                    UsersCount = _context.Users.Count(u => u.BranchId == b.Id)
                })
                .ToListAsync();
        }
    }
}