using System.ComponentModel.DataAnnotations;

namespace SearchTool_ServerSide.Dtos.PermissionDtos
{
    public class UpdatePermissionDto
    {
        [Required]
        public string ? Name { get; set; } = default!;

       [MaxLength(1000)]
        public string ? Description { get; set; }

        public string ? Url { get; set; }

        public string ? HttpMethod { get; set; }
    }
}