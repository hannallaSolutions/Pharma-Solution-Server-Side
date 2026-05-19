namespace SearchTool_ServerSide.Features
{
    public class FeatureCatalogItem
    {
        public string FeatureKey { get; set; } = string.Empty;

        public string FeatureName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string SelectionType { get; set; } = "SingleChoice";

        public List<string> DefaultOptionKeys { get; set; } = new();

        public List<FeatureOptionCatalogItem> Options { get; set; } = new();
    }

    public class FeatureOptionCatalogItem
    {
        public string OptionKey { get; set; } = string.Empty;

        public string OptionName { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}