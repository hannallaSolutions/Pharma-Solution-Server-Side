namespace SearchTool_ServerSide.Dtos.BranchIntelligenceDto
{
    public class BranchExecutiveCardDto
    {
        // "BestPerforming" | "NeedsAttention" | "HighestEfficiency" | "HighestWorkload"
        public string CardType { get; set; } = string.Empty;

        public long? BranchId { get; set; }
        public string? BranchName { get; set; }

        public string PrimaryMetricLabel { get; set; } = string.Empty;
        public decimal? PrimaryMetricValue { get; set; }

        public string SecondaryMetricLabel { get; set; } = string.Empty;
        public decimal? SecondaryMetricValue { get; set; }

        public string Explanation { get; set; } = string.Empty;
    }
}
