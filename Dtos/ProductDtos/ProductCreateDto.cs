using System.ComponentModel.DataAnnotations;

namespace SearchTool_ServerSide.Dtos.ProductDtos
{
    public class ProductCreateDto
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Range(0, 999999)]
        public decimal Price { get; set; }

        [Range(0, 999999)]
        public int Stock { get; set; }

        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;
    }
    
}
