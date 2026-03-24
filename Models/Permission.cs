
namespace SearchTool_ServerSide.Models
{
    public class Permission
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public string Url { get; set; } = null!;

        public string HttpMethod { get; set; } = null!;

        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}