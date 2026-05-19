namespace SearchTool_ServerSide.Features
{
    public static class FeatureCatalog
    {
        public const string SingleChoice = "SingleChoice";
        public const string MultiChoice = "MultiChoice";

        public static readonly List<FeatureCatalogItem> Items = new()
        {
            new FeatureCatalogItem
            {
                FeatureKey = FeatureKeys.ScriptsDataInput,
                FeatureName = "Scripts Data Input",
                Description = "Controls how scripts data can be entered into the system.",
                SelectionType = MultiChoice,
                DefaultOptionKeys = new List<string>
                {
                    FeatureOptionKeys.ScriptsDataInput.ExcelUpload
                },
                Options = new List<FeatureOptionCatalogItem>
                {
                    new FeatureOptionCatalogItem
                    {
                        OptionKey = FeatureOptionKeys.ScriptsDataInput.ExcelUpload,
                        OptionName = "Excel Upload",
                        Description = "Upload scripts using Excel files."
                    },
                    new FeatureOptionCatalogItem
                    {
                        OptionKey = FeatureOptionKeys.ScriptsDataInput.ManualForm,
                        OptionName = "Manual Form",
                        Description = "Enter scripts manually using a form."
                    },
                    new FeatureOptionCatalogItem
                    {
                        OptionKey = FeatureOptionKeys.ScriptsDataInput.SharePointSync,
                        OptionName = "SharePoint Sync",
                        Description = "Sync scripts from SharePoint."
                    },
                    new FeatureOptionCatalogItem
                    {
                        OptionKey = FeatureOptionKeys.ScriptsDataInput.ApiIntegration,
                        OptionName = "API Integration",
                        Description = "Import scripts through API integration."
                    }
                }
            },

            new FeatureCatalogItem
            {
                FeatureKey = FeatureKeys.PriceFilter,
                FeatureName = "Price Filter",
                Description = "Controls which pricing strategy should be used.",
                SelectionType = SingleChoice,
                DefaultOptionKeys = new List<string>
                {
                    FeatureOptionKeys.PriceFilter.AverageCost
                },
                Options = new List<FeatureOptionCatalogItem>
                {
                    new FeatureOptionCatalogItem
                    {
                        OptionKey = FeatureOptionKeys.PriceFilter.AverageCost,
                        OptionName = "Average Cost",
                        Description = "Use average acquisition cost."
                    },
                    new FeatureOptionCatalogItem
                    {
                        OptionKey = FeatureOptionKeys.PriceFilter.LatestPrice,
                        OptionName = "Latest Price",
                        Description = "Use the latest available price."
                    },
                    new FeatureOptionCatalogItem
                    {
                        OptionKey = FeatureOptionKeys.PriceFilter.HighestProfit,
                        OptionName = "Highest Profit",
                        Description = "Use the option with the highest expected profit."
                    },
                    new FeatureOptionCatalogItem
                    {
                        OptionKey = FeatureOptionKeys.PriceFilter.ManualSelection,
                        OptionName = "Manual Selection",
                        Description = "Use manually selected pricing behavior."
                    }
                }
            },

            new FeatureCatalogItem
            {
                FeatureKey = FeatureKeys.DiseaseVisibility,
                FeatureName = "Disease Visibility",
                Description = "Controls how diseases are visible to doctors and users.",
                SelectionType = SingleChoice,
                DefaultOptionKeys = new List<string>
                {
                    FeatureOptionKeys.DiseaseVisibility.AllDoctors
                },
                Options = new List<FeatureOptionCatalogItem>
                {
                    new FeatureOptionCatalogItem
                    {
                        OptionKey = FeatureOptionKeys.DiseaseVisibility.AllDoctors,
                        OptionName = "All Doctors",
                        Description = "All doctors can see all diseases."
                    },
                    new FeatureOptionCatalogItem
                    {
                        OptionKey = FeatureOptionKeys.DiseaseVisibility.CustomByDoctor,
                        OptionName = "Customize By Doctor",
                        Description = "Choose diseases allowed for each doctor."
                    },
                    new FeatureOptionCatalogItem
                    {
                        OptionKey = FeatureOptionKeys.DiseaseVisibility.OwnOnly,
                        OptionName = "Own Only",
                        Description = "Each doctor sees only their own diseases."
                    }
                }
            }
        };
    }
}