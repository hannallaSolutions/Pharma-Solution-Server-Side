namespace SearchTool_ServerSide.Dtos.BranchIntelligenceDto
{
    // Internal aggregate used between the repository and service layers
    // when building a single branch's BranchDetailDto overview section.
    public class BranchOverviewAggregateDto
    {
        public int TotalScripts { get; set; }
        public decimal TotalNetProfit { get; set; }
        public int NegativeScriptCount { get; set; }
    }
}
