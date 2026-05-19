namespace SearchTool_ServerSide.Models
{
    public class MainCompanyFeatureSetting
    {
        public int Id { get; set; }

        public int MainCompanyId { get; set; }

        public string FeatureKey { get; set; } = string.Empty;

        public string SelectedOptionKeysJson { get; set; } = "[]";

        public bool IsEnabled { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public int? UpdatedByUserId { get; set; }
    }
}