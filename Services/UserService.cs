using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using Microsoft.IdentityModel.Tokens;
using SearchTool_ServerSide.Dtos.UserDtos;
using SearchTool_ServerSide.Models;
using SearchTool_ServerSide.Repository;
using ServerSide;
using SearchTool_ServerSide.Dtos.BranchDTOs;

namespace SearchTool_ServerSide.Services
{
    public class UserSevice(UserRepository _userRepository, IMapper _mapper, JwtOptions jwtOptions,
       MainCompanyRepository _mainCompanyRepository, BranchRepository _branchRepository, LogRepository _logRepository)
    {
        internal async Task<UserReadDto> Register(UserAddDto userAddDto)
        {
            var user = _mapper.Map<User>(userAddDto);

            user = await _userRepository.Add(user);
            var userReadDto = _mapper.Map<UserReadDto>(user);
            return userReadDto;
        }


internal async Task<UserReadDto> RegisterDemo(DemoRegisterDto dto)
{
    var demoCount = await _userRepository.CountDemoUsers();
    var n = demoCount + 1;

    var mainCompany = new MainCompany
    {
        Name = $"Demo Pharma #{n}",
        SpecialtyId = 1,
        ClassTypeId = 2
    };

var createdMainCompany = await _mainCompanyRepository.CreateAsync(mainCompany);

    if (createdMainCompany == null)
        throw new Exception("Failed to create demo main company.");

    
var branchDto = new CreateBranchDto
{
    Name = $"Demo Branch #{n}",
    Location = "Demo Location",
    Code = $"DEMO-BR-{DateTime.UtcNow.Ticks}",
    MainCompanyId = createdMainCompany.Id
};
    var createdBranch = await _branchRepository.CreateAsync(branchDto);

   if (createdBranch == null)
        throw new Exception("Failed to create demo main company.");
        
    var user = new User
    {
        Email = dto.Email,
        Name = dto.Email,
        ShortName = dto.Email,
        Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
        Role = Role.Demo,
        BranchId = createdBranch.Id
    };

    user = await _userRepository.Add(user);

    return _mapper.Map<UserReadDto>(user);
}


        internal async Task<UserReadDto> GetUserByEmail(string email)
        {
            var user = await _userRepository.GetUserByEmail(email);
            var userReadDto = _mapper.Map<UserReadDto>(user);
            return userReadDto;
        }

        internal async Task<(string accessToken, string refreshToken, string userId, string branchId, string classType)?> Login(UserLoginDto userLoginDto)
        {

            var user = await _userRepository.GetUserByEmail(userLoginDto.Email);
            if (user == null)
            {
                return null;
            }
            var mainCompany = await _branchRepository.GetMainCompanyByBranchId(user.BranchId);
            Console.WriteLine("mainCompany : " + mainCompany);
            if (mainCompany == null)
            {
                return null;
            }
            if (!BCrypt.Net.BCrypt.Verify(userLoginDto.Password, user.Password))
            {
                return null;
            }
            var log = new Log
            {
                UserEmail = user.Email,
                Date = DateTime.UtcNow,
                Action = "Login",
                Description = $"User logged in successfully",
                IpAddress = "Unknown",
                DeviceInfo = "Unknown"

            };
            await _logRepository.Add(log);

            var accessToken = TokenGenerate(user, expiresInMinutes: 1);
            // Refresh token now valid for 8 hours
            var refreshToken = TokenGenerate(user, expiresInMinutes: 480);
            var userId = user.Id.ToString();
            var branchId = user.BranchId.ToString();
            Console.WriteLine("mainCompany.ClassType.Name : " + mainCompany.ClassType.Name + " branchId : " + branchId + " userId : " + userId + " refreshToken : " + refreshToken);
            return (accessToken, refreshToken, userId, branchId, mainCompany.ClassType.Name ?? "ClassV1");
        }

        public string TokenGenerate(User user, int expiresInMinutes = 60, int expiresInDays = 0)
        {
            var expirationDate = DateTime.UtcNow.AddMinutes(expiresInMinutes);

            if (expiresInDays > 0)
            {
                expirationDate = DateTime.UtcNow.AddDays(expiresInDays);
            }
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = jwtOptions.Issuer,
                Audience = jwtOptions.Audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                    SecurityAlgorithms.HmacSha256),
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new(ClaimTypes.Email, user.Email),
                    new(ClaimTypes.Role, user.Role.ToString()),
                    new("BranchId", user.BranchId.ToString())
                }),
                Expires = expirationDate
            };
            var securityToken = tokenHandler.CreateToken(tokenDescriptor);
            var accessToken = tokenHandler.WriteToken(securityToken);
            return accessToken;
        }

        public async Task<UserReadDto?> GetUserById(int id)
        {
            var user = await _userRepository.GetById(id);
            var userReadDto = _mapper.Map<UserReadDto>(user);
            return userReadDto;
        }

        public async Task<(string accessToken, string refreshToken, string userId)?> Refresh(int userId)
        { 
            var user = await _userRepository.GetById(userId);
            if (user == null)
            {
                return null;
            }

            var accessToken = TokenGenerate(user, expiresInMinutes: 1);
            // Refresh token now valid for 8 hours
            var refreshToken = TokenGenerate(user, expiresInMinutes: 480);
            return (accessToken, refreshToken, userId.ToString());
        }

        internal async Task<UserReadDto> UserUpdate(int userId, UserUpdateDto userUpdateDto)
        {
            var user = await _userRepository.GetById(userId);
            if (user == null)
                return null;
            _mapper.Map(userUpdateDto, user);
            await _userRepository.Update(user);
            var userReadDto = _mapper.Map<UserReadDto>(user);
            return userReadDto;
        }

        
        internal async Task<bool> DeleteUserById(int id)
{
    return await _userRepository.Delete(id);
}

        internal async Task<ICollection<UserReadDto>> GetAllUser()
        {
            var users = await _userRepository.GetAll();
            var userReadDtos = _mapper.Map<ICollection<UserReadDto>>(users);
            return userReadDtos;
        }

        internal async Task<IEnumerable<User>> GetAllUserCrid()
        {
            var users = await _userRepository.GetAll();
            return users;
        }

        internal async Task AddAllUserData(IEnumerable<AllUserAddDto> items)
        {
            var users = _mapper.Map<IEnumerable<User>>(items);
            await _userRepository.AddAllUserData(users);
        }
        internal async Task<ICollection<UserReadDto>> GetAllUsers()
        {
            return await _userRepository.GetAllUsers();
        }
        internal async Task<bool> ResetUserPassword(string userEmail)
        {
            return await _userRepository.ResetUserPassword(userEmail);
        }
        internal async Task<EditUserDto ?> EditUser(int userId, EditUserDto  EditUserDto )
        {
            var updatedUser = await _userRepository.EditUser(userId, EditUserDto );
            if (updatedUser == null)
            {
                return null;
            }
            return EditUserDto ;
        }

        internal async Task<List<UserBranchReadDto>> GetUserBranches(int userId)
        {
            return await _userRepository.GetUserBranches(userId);
        }

        internal async Task<List<UserBranchReadDto>?> GetUserBranchesAdmin(int userId)
            => await _userRepository.GetUserBranchesAdmin(userId);

        internal async Task<(UserBranchReadDto? Result, string? Error, int StatusCode)> AssignBranchToUser(int userId, AssignBranchDto dto)
            => await _userRepository.AssignBranchToUser(userId, dto);

        internal async Task<(bool Success, string? Error, int StatusCode)> DeactivateUserBranch(int userId, int branchId)
            => await _userRepository.DeactivateUserBranch(userId, branchId);

        internal async Task<(UserBranchReadDto? Result, string? Error, int StatusCode)> SetUserDefaultBranch(int userId, int branchId)
            => await _userRepository.SetUserDefaultBranch(userId, branchId);

        internal async Task<(string? AccessToken, string? RefreshToken, int BranchId, string? Error, int StatusCode)> SwitchCurrentBranch(int userId, int branchId)
        {
            var (success, error, statusCode) = await _userRepository.SwitchCurrentBranch(userId, branchId);
            if (!success)
                return (null, null, 0, error, statusCode);

            var tokens = await Refresh(userId);
            if (tokens == null)
                return (null, null, 0, "Failed to generate token", 500);

            return (tokens.Value.accessToken, tokens.Value.refreshToken, branchId, null, 0);
        }
    }
}
