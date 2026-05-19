namespace SearchTool_ServerSide.Dtos.FeatureSettingsDTOs
{
    public class FeatureSettingsViewDto
    {
        public string FeatureKey { get; set; } = string.Empty;

        public string FeatureName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string SelectionType { get; set; } = string.Empty;

        public List<string> DefaultOptionKeys { get; set; } = new();

        public List<string> SelectedOptionKeys { get; set; } = new();

        public bool IsEnabled { get; set; }

        public List<FeatureOptionViewDto> Options { get; set; } = new();
    }

    public class FeatureOptionViewDto
    {
        public string OptionKey { get; set; } = string.Empty;

        public string OptionName { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}