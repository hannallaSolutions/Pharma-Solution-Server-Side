namespace SearchTool_ServerSide.Models
{
    public class RolePermission
    {
        public Role Role { get; set; }     // Enum stored as int
        public int PermissionId { get; set; } // foreign key reference to Permission entity
        public Permission Permission { get; set; } = default!;  // Navigation property
    }
}
