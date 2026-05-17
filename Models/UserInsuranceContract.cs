namespace SearchTool_ServerSide.Models
{
    public class UserInsuranceContract
    {
        public int Id { get; set; }

        // User / prescriber / physician
        public int UserId { get; set; }

        // Insurance / Rx plan id
        public int InsuranceRxId { get; set; }
        public User User { get; set; }
        public InsuranceRx InsuranceRx {  set; get; }

        // AWP, ASP, MAC, FIXED
        public string ReimbursementType { get; set; } = string.Empty;

        // AWP formula example:
        // AWP - 22% + dispensing fee
        public decimal? AwpDiscountPercent { get; set; }

        // ASP formula example:
        // ASP + 6%
        public decimal? AspMarkupPercent { get; set; }

        // MAC formula:
        // flat maximum allowed amount
        public decimal? MacPrice { get; set; }

        // Optional fixed reimbursement value
        public decimal? FixedReimbursementAmount { get; set; }

        // Fee added to reimbursement
        public decimal? DispensingFee { get; set; }

        // Optional patient payment/copay if contract defines it
        public decimal? ExpectedPatientPay { get; set; }

        // Contract dates
        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }

        public bool IsActive { get; set; } = true;

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}