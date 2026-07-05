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

        // Returns null when the user does not exist; empty list when no active branches yet.
        internal async Task<List<UserBranchReadDto>?> GetUserBranchesAdmin(int userId)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists) return null;

            return await _context.UserBranches
                .Where(ub => ub.UserId == userId && ub.IsActive)
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
        }

        internal async Task<(UserBranchReadDto? Result, string? Error, int StatusCode)> AssignBranchToUser(int userId, AssignBranchDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return (null, "User not found", 404);

            var branch = await _context.Branches
                .Include(b => b.MainCompany)
                .FirstOrDefaultAsync(b => b.Id == dto.BranchId);
            if (branch == null) return (null, "Branch not found", 404);

            var existing = await _context.UserBranches
                .FirstOrDefaultAsync(ub => ub.UserId == userId && ub.BranchId == dto.BranchId);

            if (existing != null && existing.IsActive)
                return (null, "Branch already assigned to this user", 409);

            var activeCount = await _context.UserBranches
                .CountAsync(ub => ub.UserId == userId && ub.IsActive);

            // Force default when this is the first active branch or caller requested it
            bool makeDefault = dto.IsDefault || activeCount == 0;

            if (existing != null)
            {
                existing.IsActive    = true;
                existing.IsDefault   = makeDefault;
                existing.AssignedAt  = DateTime.UtcNow;
            }
            else
            {
                _context.UserBranches.Add(new UserBranch
                {
                    UserId      = userId,
                    BranchId    = dto.BranchId,
                    IsDefault   = makeDefault,
                    IsActive    = true,
                    AssignedAt  = DateTime.UtcNow
                });
            }

            if (makeDefault)
            {
                var otherDefaults = await _context.UserBranches
                    .Where(ub => ub.UserId == userId && ub.BranchId != dto.BranchId && ub.IsDefault)
                    .ToListAsync();
                foreach (var ub in otherDefaults)
                    ub.IsDefault = false;

                user.BranchId = dto.BranchId;
            }

            await _context.SaveChangesAsync();

            return (new UserBranchReadDto
            {
                BranchId        = branch.Id,
                BranchName      = branch.Name,
                BranchCode      = branch.Code,
                MainCompanyId   = branch.MainCompanyId,
                MainCompanyName = branch.MainCompany?.Name ?? string.Empty,
                IsDefault       = makeDefault,
                IsActive        = true
            }, null, 0);
        }

        internal async Task<(bool Success, string? Error, int StatusCode)> DeactivateUserBranch(int userId, int branchId)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists) return (false, "User not found", 404);

            var assignment = await _context.UserBranches
                .FirstOrDefaultAsync(ub => ub.UserId == userId && ub.BranchId == branchId);
            if (assignment == null) return (false, "Branch assignment not found", 404);

            if (!assignment.IsActive) return (false, "Branch already inactive", 409);

            if (assignment.IsDefault)
                return (false, "Cannot remove the default branch. Set another branch as default first.", 409);

            var activeCount = await _context.UserBranches
                .CountAsync(ub => ub.UserId == userId && ub.IsActive);
            if (activeCount <= 1)
                return (false, "Cannot remove the user's only active branch", 409);

            assignment.IsActive = false;
            await _context.SaveChangesAsync();
            return (true, null, 0);
        }

        internal async Task<(UserBranchReadDto? Result, string? Error, int StatusCode)> SetUserDefaultBranch(int userId, int branchId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return (null, "User not found", 404);

            var assignment = await _context.UserBranches
                .Include(ub => ub.Branch)
                    .ThenInclude(b => b.MainCompany)
                .FirstOrDefaultAsync(ub => ub.UserId == userId && ub.BranchId == branchId);
            if (assignment == null) return (null, "Branch assignment not found", 404);

            if (!assignment.IsActive)
                return (null, "Cannot set an inactive branch as default", 409);

            if (!assignment.IsDefault)
            {
                var otherDefaults = await _context.UserBranches
                    .Where(ub => ub.UserId == userId && ub.BranchId != branchId && ub.IsDefault)
                    .ToListAsync();
                foreach (var ub in otherDefaults)
                    ub.IsDefault = false;

                assignment.IsDefault = true;
                user.BranchId = branchId;
                await _context.SaveChangesAsync();
            }

            return (new UserBranchReadDto
            {
                BranchId        = assignment.BranchId,
                BranchName      = assignment.Branch.Name,
                BranchCode      = assignment.Branch.Code,
                MainCompanyId   = assignment.Branch.MainCompanyId,
                MainCompanyName = assignment.Branch.MainCompany?.Name ?? string.Empty,
                IsDefault       = true,
                IsActive        = true
            }, null, 0);
        }

        // Switches the user's current branch (Users.BranchId) only. Does not touch UserBranch.IsDefault.
        internal async Task<(bool Success, string? Error, int StatusCode)> SwitchCurrentBranch(int userId, int branchId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return (false, "User not found", 404);

            var branchExists = await _context.Branches.AnyAsync(b => b.Id == branchId);
            if (!branchExists) return (false, "Branch not found", 404);

            var hasActiveAccess = await _context.UserBranches.AnyAsync(ub =>
                ub.UserId == userId && ub.BranchId == branchId && ub.IsActive);
            if (!hasActiveAccess) return (false, "User does not have active access to this branch", 403);

            if (user.BranchId != branchId)
            {
                user.BranchId = branchId;
                await _context.SaveChangesAsync();
            }

            return (true, null, 0);
        }
    }

}