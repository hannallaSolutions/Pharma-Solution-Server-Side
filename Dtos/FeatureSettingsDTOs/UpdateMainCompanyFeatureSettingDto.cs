using System.ComponentModel.DataAnnotations;

namespace SearchTool_ServerSide.Dtos.FeatureSettingsDTOs
{
    public class UpdateMainCompanyFeatureSettingDto
    {
        [Required]
        public List<string> SelectedOptionKeys { get; set; } = new();

        public bool IsEnabled { get; set; } = true;
    }
}