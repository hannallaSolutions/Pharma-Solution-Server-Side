namespace SearchTool_ServerSide.Dtos.DrugWholesalerPrescriberDtos
{
    public class AddDrugWholesalerPrescriberDto
    {
        public int DrugId { get; set; }
        public int WholesalerId { get; set; }
        public int PrescriberId { get; set; }

        public decimal Price { get; set; }
        public DateTime PriceDate { get; set; }

        public decimal? AWP { get; set; }
        public decimal? WAC { get; set; }
        public decimal? ASP { get; set; }
        public decimal? MAC { get; set; }

        public string? BillingUnit { get; set; }
        public string? DrugClass { get; set; }
        public string? QuarterYear { get; set; }

        public string? SourceFileName { get; set; }
        public string? SourcePath { get; set; }
    }


    public class PrescriberOptionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
}
