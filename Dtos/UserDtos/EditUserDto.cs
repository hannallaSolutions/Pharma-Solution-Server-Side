

using SearchTool_ServerSide.Models;

namespace SearchTool_ServerSide.Dtos.UserDtos
{

public class EditUserDto
{
    public string Email { get; set; }
    public string Name { get; set; }
    public int BranchId { get; set; }
    public Role Role { get; set; }
}

}