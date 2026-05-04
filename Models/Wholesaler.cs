namespace SearchTool_ServerSide.Models
{
    public class Wholesaler
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        // Examples:
        // CuraScript
        // Besse/Cencora
        // Morris & Dickson

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<DrugWholesaler> DrugWholesalers { get; set; } = new List<DrugWholesaler>();
        public ICollection<UserDrugWholesaler> UserDrugWholesalers { get; set; } = new List<UserDrugWholesaler>();
    }
}
