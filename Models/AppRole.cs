using SearchTool_ServerSide.Models;

namespace Pharma_Solution_Server_Side.Models;


public class AppRole
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}