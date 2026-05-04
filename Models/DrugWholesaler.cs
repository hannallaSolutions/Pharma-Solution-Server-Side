namespace SearchTool_ServerSide.Models
{
    public class DrugWholesaler
    {
        public int Id { get; set; }

        public int DrugId { get; set; }
        public Drug Drug { get; set; } = null!;

        public int WholesalerId { get; set; }
        public Wholesaler Wholesaler { get; set; } = null!;

        public decimal Price { get; set; }

        public DateTime PriceDate { get; set; }
        public string? QuarterYear { get; set; }

        public string? SourceFileName { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
