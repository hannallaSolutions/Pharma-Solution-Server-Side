namespace SearchTool_ServerSide.Dtos.BranchIntelligenceDto
{
    public class BranchMonthlyProfitPointDto
    {
        // "yyyy-MM"
        public string Month { get; set; } = string.Empty;
        public decimal NetProfit { get; set; }
        public int TotalScripts { get; set; }
    }
}
