using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SearchTool_ServerSide.Data;
using SearchTool_ServerSide.Dtos.UserDtos;
using SearchTool_ServerSide.Models;
using System.Collections.Generic;

namespace SearchTool_ServerSide.Repository
{
    public class UserRepository : GenericRepository<User>
    {
        private readonly SearchToolDBContext _context;
        private readonly IMapper _mapper;
        public UserRepository(SearchToolDBContext context, IMapper mapper) : base(context)
        {
            _context = context;
            _mapper = mapper;
        }

        internal async Task AddAllUserData(IEnumerable<User> items)
        {
            var localUsers = await _context.Users
                .Select(u => new { u.Id, u.Email })
                .ToListAsync();

            var emailToUserId = localUsers.ToDictionary(u => u.Email.ToLower(), u => u.Id);

            int skipped = 0, inserted = 0, updated = 0;

            foreach (var user in items)
            {
                if (string.IsNullOrWhiteSpace(user.Email) || user.Password == "DefaultPass123")
                {
                    skipped++;
                    continue;
                }

                var emailKey = user.Email.ToLower();

                if (emailToUserId.TryGetValue(emailKey, out int existingUserId))
                {
                    // Update existing user (do not update Id)
                    var existing = await _context.Users.FindAsync(existingUserId);
                    if (existing != null)
                    {
                        existing.Email = user.Email;
                        existing.ShortName = user.ShortName;
                        existing.Name = user.Name;
                        existing.Password = user.Password;
                        existing.BranchId = user.BranchId;
                        existing.Role = user.Role;
                        updated++;
                    }
                }
                else
                {
                    // New user: reset Id to avoid PK conflict
                    user.Id = 0;
                    await _context.Users.AddAsync(user);
                    inserted++;
                }
            }
            await _context.SaveChangesAsync();
            // Optionally: return stats or log them
        }

        internal async Task<User?> GetUserByEmail(string email)
        {

            return await _context.Users
                .FirstOrDefaultAsync(x => x.Email.ToLower() == email.ToLower());
        }

        internal async Task<ICollection<UserReadDto>> GetAllUsers()
        {
            return await _context.Users.Include(x => x.Branch)
                .Select(user => _mapper.Map<UserReadDto>(user))
                .ToListAsync();
        }

        internal async Task<bool> ResetUserPassword(string userEmail)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.ToLower());
            if (user == null)
            {
                return false;
            }
            var cleanName = string.Concat(user.Name.Where(c => !char.IsWhiteSpace(c)));
            var newHashedPassword = BCrypt.Net.BCrypt.HashPassword($"{cleanName}@HannaWest2025");
            user.Password = newHashedPassword;
            await _context.SaveChangesAsync();
            return true;
        }
      
      public async Task<int> CountDemoUsers()
{
    return await _context.Users
        .CountAsync(u => u.Role == Role.Demo);
}

internal async Task<User?> EditUser(int userId, EditUserDto dto)
{
    var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
    if (user == null) return null;

    user.Email = dto.Email;
    user.Name = dto.Name;
    user.BranchId = dto.BranchId;
    user.Role = dto.Role;

    await _context.SaveChangesAsync();
    return user;
}

// delete user by id
        internal async Task<bool> Delete(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        internal async Task<List<UserBranchReadDto>> GetUserBranches(int userId)
        {
            var rows = await _context.UserBranches
                .Where(ub => ub.UserId == userId)
                .Include(ub => ub.Branch)
                    .ThenInclude(b => b.MainCompany)
                .Select(ub => new UserBranchReadDto
                {
                    BranchId        = ub.BranchId,
                    BranchName      = ub.Branch.Name,
                    BranchCode      = ub.Branch.Code,
                    MainCompanyId   = ub.Branch.MainCompanyId,
                    MainCompanyName = ub.Branch.MainCompany != null ? ub.Branch.MainCompany.Name : string.Empty,
                    IsDefault       = ub.IsDefault,
                    IsActive        = ub.IsActive
                })
                .ToListAsync();

            // Fallback: UserBranches not yet populated — read from Users.BranchId
            if (rows.Count == 0)
            {
                var user = await _context.Users
                    .Include(u => u.Branch)
                        .ThenInclude(b => b.MainCompany)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user?.Branch != null)
                {
                    rows.Add(new UserBranchReadDto
                    {
                        BranchId        = user.BranchId,
                        BranchName      = user.Branch.Name,
                        BranchCode      = user.Branch.Code,
                        MainCompanyId   = user.Branch.MainCompanyId,
                        MainCompanyName = user.Branch.MainCompany?.Name ?? string.Empty,
                        IsDefault       = true,
                        IsActive        = true
                    });
                }
            }

            return rows;
        }
    }

}