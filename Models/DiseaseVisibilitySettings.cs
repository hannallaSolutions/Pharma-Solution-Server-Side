using SearchTool_ServerSide.Models.Enums;

namespace SearchTool_ServerSide.Models
{
    public class DiseaseVisibilitySettings : ServerSide.Model.IEntity
    {
        public int Id { get; set; } = 1; // single row
        public DiseaseVisibilityMode Mode { get; set; } = DiseaseVisibilityMode.AllDoctors;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public int? UpdatedByUserId { get; set; } // optional
    }
}
