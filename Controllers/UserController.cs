using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SearchTool_ServerSide.Authentication;
using SearchTool_ServerSide.Dtos.UserDtos;
using SearchTool_ServerSide.Models;
using SearchTool_ServerSide.Repository;
using SearchTool_ServerSide.Services;
using SearchTool_ServerSide.Authorization;
using System.Collections.Generic;

namespace SearchTool_ServerSide.Controllers
{
    [ApiController]
    [Route("user")]
    public class UserController(UserSevice _userService, UserAccessToken userAccessToken, LogRepository _logRepository) : ControllerBase
    {
        [HttpPost("Register")]
        
        public async Task<IActionResult> Register([FromBody] UserAddDto userAddDto)
        {
            Console.WriteLine("Hi : " + userAddDto.Email);
            var oldUser = await _userService.GetUserByEmail(userAddDto.Email);
            if (oldUser != null)
            {
                return Conflict(new { message = "This email already exists." });
            }
            userAddDto.Password = BCrypt.Net.BCrypt.HashPassword(userAddDto.Password);
            var user = await _userService.Register(userAddDto);
            return Ok(user);
        }


[HttpPost("RegisterDemo")]
[AllowAnonymous]
public async Task<IActionResult> RegisterDemo([FromBody] DemoRegisterDto dto)
{
    var oldUser = await _userService.GetUserByEmail(dto.Email);

            if (oldUser != null)
            {
                return Conflict(new { message = "This email already exists." });
            }

            dto.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var user = await _userService.RegisterDemo(dto);


            return Ok(new
            {
                message = "Demo user registered successfully",
                user
            });
        }

        [HttpPost("login"),AllowAnonymous]
        public async Task<IActionResult> Login(UserLoginDto userLoginDto)
        {
            var tokens = await _userService.Login(userLoginDto);
            if (tokens == null)
                return Unauthorized(new { message = "Invalid email or password" });
            var user = await _userService.GetUserById(int.Parse(tokens.Value.userId));
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true, // required for SameSite=None
                SameSite = SameSiteMode.None, // allow cross-site cookie
                Expires = DateTime.UtcNow.AddDays(1)
            };
            Response.Cookies.Append("refreshToken", tokens.Value.refreshToken, cookieOptions);

            return Ok(new { accessToken = tokens.Value.accessToken, role = user.Role.ToString(), classType = tokens.Value.classType, userId = tokens.Value.userId, branchId = tokens.Value.branchId });
        }

        [HttpGet("token-test")]
        [Authorize]
        public IActionResult TokenTest()
        {
            return Ok("Authorized");
        }
        [HttpPost("access-token"),AllowAnonymous]
        public async Task<IActionResult> GenerateToken()
        {
            // Get refresh token from cookies
            var refreshToken = Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(refreshToken))
            {
                return Unauthorized("No refresh token found");
            }

            // Validate and extract user from refresh token
            Console.WriteLine("refresh token: " + refreshToken);
            var user = userAccessToken.ValidateRefreshToken(refreshToken);
            if (user == null || user.UserId==null)
            {
                return Unauthorized("Invalid refresh token");
            }

            // Generate new access & refresh tokens
            var tokens = await _userService.Refresh(int.Parse(user.UserId));
            if (tokens == null)
            {
                return BadRequest("Failed to refresh token");
            }

            // Set new refresh token in secure cookies
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true, // Prevent JavaScript access
                Secure = true, // HTTPS only
                SameSite = SameSiteMode.Strict, // Prevent CSRF
                Expires = DateTime.UtcNow.AddDays(1) // Expiration time
            };
            Response.Cookies.Append("refreshToken", tokens.Value.refreshToken, cookieOptions);
            return Ok(new
            {
                accessToken = tokens.Value.accessToken
            });
        }

        [HttpGet("UserById"), Authorize]
        public async Task<IActionResult> GetUserById()
        {
            var userData = userAccessToken.tokenData();
            if (userData == null || string.IsNullOrEmpty(userData.UserId))
            {
                return Unauthorized("Invalid or missing token data");
            }

            if (!int.TryParse(userData.UserId, out int userId))
            {
                return BadRequest("Invalid user ID format");
            }

            var user = await _userService.GetUserById(userId);
            if (user == null)
            {
                return NotFound("User not found");
            }
            return Ok(user);
        }

        [HttpPut("UpdateUser")]
        public async Task<IActionResult> UpdateUser(UserUpdateDto userUpdateDto)
        {
            var userData = userAccessToken.tokenData();
            if (userData == null || string.IsNullOrEmpty(userData.UserId))
            {
                return Unauthorized("Invalid or missing token data");
            }

            if (!int.TryParse(userData.UserId, out int userId))
            {
                return BadRequest("Invalid user ID format");
            }

            if (userUpdateDto.Password != null)
            {
                userUpdateDto.Password = BCrypt.Net.BCrypt.HashPassword(userUpdateDto.Password);
            }
            var user = await _userService.UserUpdate(userId, userUpdateDto);
            return Ok(user);
        }
        
        [HttpDelete("id")]
public async Task<IActionResult> DeleteUserById([FromQuery] int userId)
{
    var result = await _userService.DeleteUserById(userId);

    if (!result)
    {
        return NotFound(new { message = "User not found" });
    }

    return Ok(new { message = "User deleted successfully" });
}
        [HttpGet]
        public async Task<IActionResult> GetAllUser()
        {
            var users = await _userService.GetAllUser();
            return Ok(users);
        }
        [HttpGet("allCrid"), AllowAnonymous]
        public async Task<IActionResult> GetAllUserCrid()
        {
            var users = await _userService.GetAllUserCrid();
            return Ok(users);
        }
        [HttpPost("InsertUserData"), AllowAnonymous]
        public async Task<IActionResult> InsertUserData([FromBody] IEnumerable<AllUserAddDto> Items)
        {
            await _userService.AddAllUserData(Items);
            return Ok("Users Added successfully to DataBase :)");
        }
        [HttpGet("Logout")]
        public IActionResult LogOut()
        {

            return Ok("LogOut Success");
        }
        [HttpGet("AllUsers") ,Authorize]
      //  [HasPermission("GetAllUsers + ResetUserPassword")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userService.GetAllUsers();
            return Ok(users);
        }
        [HttpPost("ResetPassword"), Authorize]
     //   [HasPermission("GetAllUsers + ResetUserPassword")]

        public async Task<IActionResult> ResetUserPassword([FromQuery] string userEmail)
        {
            var result = await _userService.ResetUserPassword(userEmail);
            if (!result)
            {
                return BadRequest("User not found");
            }
            return Ok("Password reset successfully");
        }
       
        [HttpPut("EditUser"), Authorize]
public async Task<IActionResult> EditUser([FromQuery] int userId, [FromBody] EditUserDto dto)
{
    var updatedUser = await _userService.EditUser(userId, dto);
    if (updatedUser == null) return NotFound("User not found");
    return Ok(updatedUser);
}

        [HttpGet("me/branches")]
        [Authorize]
        public async Task<IActionResult> GetMyBranches()
        {
            var userData = userAccessToken.tokenData();
            if (userData == null || string.IsNullOrEmpty(userData.UserId))
                return Unauthorized(new { message = "Invalid or missing token data" });

            if (!int.TryParse(userData.UserId, out int userId))
                return BadRequest(new { message = "Invalid user ID format" });

            var branches = await _userService.GetUserBranches(userId);
            return Ok(branches);
        }

        [HttpGet("{userId:int}/branches")]
        [Authorize]
        public async Task<IActionResult> GetUserBranches(int userId)
        {
            var branches = await _userService.GetUserBranchesAdmin(userId);
            if (branches == null)
                return NotFound(new { message = "User not found" });
            return Ok(branches);
        }

        [HttpPost("{userId:int}/branches")]
        [Authorize]
        public async Task<IActionResult> AssignBranch(int userId, [FromBody] AssignBranchDto dto)
        {
            var (result, error, statusCode) = await _userService.AssignBranchToUser(userId, dto);
            if (error != null)
                return StatusCode(statusCode, new { message = error });
            return StatusCode(201, result);
        }

        [HttpDelete("{userId:int}/branches/{branchId:int}")]
        [Authorize]
        public async Task<IActionResult> DeactivateBranch(int userId, int branchId)
        {
            var (_, error, statusCode) = await _userService.DeactivateUserBranch(userId, branchId);
            if (error != null)
                return StatusCode(statusCode, new { message = error });
            return Ok(new { message = "Branch deactivated" });
        }

        [HttpPut("{userId:int}/branches/{branchId:int}/default")]
        [Authorize]
        public async Task<IActionResult> SetDefaultBranch(int userId, int branchId)
        {
            var (result, error, statusCode) = await _userService.SetUserDefaultBranch(userId, branchId);
            if (error != null)
                return StatusCode(statusCode, new { message = error });
            return Ok(result);
        }
    }
}
