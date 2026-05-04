namespace SearchTool_ServerSide.Models
{
    public class UserPricingPermission
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public bool CanViewPricing { get; set; } = false;

        public bool CanViewOwnPrescriberPrices { get; set; } = true;

        public bool CanViewOtherPrescriberPrices { get; set; } = false;

        public bool CanUploadWholesalerPrices { get; set; } = false;

        public bool CanEditWholesalerPrices { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
