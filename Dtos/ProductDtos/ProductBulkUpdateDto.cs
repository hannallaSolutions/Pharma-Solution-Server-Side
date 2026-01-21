using System.ComponentModel.DataAnnotations;

namespace SearchTool_ServerSide.Dtos.ProductDtos
{
public class ProductBulkUpdateDto
{
    public int Id { get; set; }
    public ProductUpdateDto Dto { get; set; } = new();
}

}