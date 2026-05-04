namespace SearchTool_ServerSide.Models
{
    public class DrugWholesalerPrescriber
    {
        public int Id { get; set; }

        // Core relations
        public int DrugId { get; set; }
        public Drug Drug { get; set; } = null!;

        public int WholesalerId { get; set; }
        public Wholesaler Wholesaler { get; set; } = null!;

        public int PrescriberId { get; set; }
        public User Prescriber { get; set; } = null!;

        // 🔴 MAIN VALUE (ACQ)
        public decimal Price { get; set; }

        // 🟡 Snapshot fields (VERY IMPORTANT)
        // These come from your Excel and help with history/debugging
        public decimal? AWP { get; set; }
        public decimal? WAC { get; set; }
        public decimal? ASP { get; set; }
        public decimal? MAC { get; set; }

        public string? BillingUnit { get; set; }
        public string? DrugClass { get; set; }

        // 🟢 Time tracking (CRITICAL)
        public DateTime PriceDate { get; set; }   // actual effective date
        public string? QuarterYear { get; set; }  // from Excel

        // 🟣 Source tracking
        public string? SourceFileName { get; set; }
        public string? SourcePath { get; set; }

        // 🟢 Control
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
