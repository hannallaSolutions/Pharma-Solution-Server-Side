using System.ComponentModel.DataAnnotations;

namespace SearchTool_ServerSide.Models
{
    public class Permission
    {
        public int Id { get; set;}

        [Required, MaxLength(200)]
        public required string Name { get; set; }
 
        [MaxLength(1000)]
        public string ? Description { get; set; }

           // create navigation properties from Permission to RolePermission , this means each Permission can be linked to multiple RolePermissions
          public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

    //    public ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
    }
}