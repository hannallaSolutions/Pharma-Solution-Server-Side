using SearchTool_ServerSide.Models;

namespace SearchTool_ServerSide.Dtos.UserDtos
{
    public class UserReadDto
{
    public int Id { get; set; }
    public required string Email { get; set; }
    public required string Name { get; set; }
    public int BranchId { get; set; }
    public string BranchName { get; set; }   // ← add this
    public Role Role { get; set; } = Role.Pharmacist;
}
}