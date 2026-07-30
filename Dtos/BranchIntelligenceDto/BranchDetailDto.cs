namespace SearchTool_ServerSide.Dtos.BranchIntelligenceDto
{
    public class BranchDetailDto
    {
        public long BranchId { get; set; }
        public string? BranchCode { get; set; }
        public string? BranchName { get; set; }

        public int TotalScripts { get; set; }
        public decimal TotalNetProfit { get; set; }
        public decimal? ProfitPerScript { get; set; }
        public int NegativeScriptCount { get; set; }
        public decimal NegativeScriptPercent { get; set; }

        public List<BranchTopDrugDto> TopDrugs { get; set; } = new();
        public List<BranchTopTherapeuticClassDto> TopTherapeuticClasses { get; set; } = new();
        public List<BranchMonthlyProfitPointDto> MonthlyNetProfitTrend { get; set; } = new();
    }
}
