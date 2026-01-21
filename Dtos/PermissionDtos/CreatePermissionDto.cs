using System.ComponentModel.DataAnnotations;

namespace SearchTool_ServerSide.Dtos.PermissionDtos
{
    public class CreatePermissionDto
    {
        [Required, MaxLength(200)]
        public string Name { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }
    }


}