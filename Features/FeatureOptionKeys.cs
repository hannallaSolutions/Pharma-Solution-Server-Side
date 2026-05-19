namespace SearchTool_ServerSide.Features
{
    public static class FeatureOptionKeys
    {
        public static class ScriptsDataInput
        {
            public const string ExcelUpload = "excel_upload";
            public const string ManualForm = "manual_form";
            public const string SharePointSync = "sharepoint_sync";
            public const string ApiIntegration = "api_integration";
        }

        public static class PriceFilter
        {
            public const string AverageCost = "average_cost";
            public const string LatestPrice = "latest_price";
            public const string HighestProfit = "highest_profit";
            public const string ManualSelection = "manual_selection";
        }

        public static class DiseaseVisibility
        {
            public const string AllDoctors = "all_doctors";
            public const string CustomByDoctor = "custom_by_doctor";
            public const string OwnOnly = "own_only";
        }
    }
}