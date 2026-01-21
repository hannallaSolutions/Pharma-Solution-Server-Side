using System.ComponentModel.DataAnnotations; // for validation attributes

namespace SearchTool_ServerSide.Dtos.PermissionDtos
{
    public class ReplaceRolePermissionsDto
    {
         [Required]
         public List<int> PermissionIds { get; set;} = new List<int>();
        
    }
}

    
