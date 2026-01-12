using System.ComponentModel.DataAnnotations; // for validation attributes

namespace SearchTool_ServerSide.Dtos.PermissionDtos
{
    public class ReplaceUserPermissionsDto
    {
         [Required]
         public List<int> PermissionIds { get; set;} = new List<int>();
        
    }
}