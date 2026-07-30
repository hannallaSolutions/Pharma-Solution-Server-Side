namespace SearchTool_ServerSide.Dtos.BranchIntelligenceDto
{
    public class BranchTopDrugDto
    {
        public string DrugName { get; set; } = string.Empty;
        public int TotalScripts { get; set; }
        public decimal TotalNetProfit { get; set; }
    }
}
