namespace SearchTool_ServerSide.Dtos.BranchIntelligenceDto
{
    public class BranchLeaderboardRowDto
    {
        public long BranchId { get; set; }
        public string? BranchCode { get; set; }
        public string? BranchName { get; set; }

        public int TotalScripts { get; set; }
        public decimal TotalNetProfit { get; set; }
        public decimal? ProfitPerScript { get; set; }

        public int NegativeScriptCount { get; set; }
        public decimal NegativeScriptPercent { get; set; }

        public decimal ShareOfCompanyScripts { get; set; }
        public decimal ShareOfCompanyProfit { get; set; }
    }
}
