namespace SearchTool_ServerSide.Dtos
{
    public class DrugPriceWithInsuranceDto
    {
        // ── Drug info ─────────────────────────────────────────────────────────
        public int DrugId { get; set; }
        public string DrugName { get; set; } = string.Empty;
        public string NDC { get; set; } = string.Empty;
        public string? Form { get; set; }
        public string? Strength { get; set; }

        // ── Insurance / contract info ─────────────────────────────────────────
        public string InsurancePlanName { get; set; } = string.Empty;
        public string ReimbursementType { get; set; } = string.Empty;
        public string ContractLabel { get; set; } = string.Empty;
        public bool NoContractFound { get; set; }

        // ── One row per wholesaler (different ACQ = different margin) ─────────
        public List<WholesalerPriceBreakdownDto> Prices { get; set; } = new();
    }

    public class WholesalerPriceBreakdownDto
    {
        // Wholesaler
        public int WholesalerId { get; set; }
        public string WholesalerName { get; set; } = string.Empty;

        // Prescriber in the branch who has this price
        public int PrescriberId { get; set; }
        public string PrescriberName { get; set; } = string.Empty;

        // ── From DrugWholesalerPrescriber ──────────────────────────────────────
        public decimal ACQ { get; set; }   // Price (acquisition cost)
        public decimal? AWP { get; set; }
        public decimal? WAC { get; set; }
        public decimal? ASP { get; set; }
        public decimal? MAC { get; set; }
        public string? BillingUnit { get; set; }
        public string? QuarterYear { get; set; }
        public DateTime PriceDate { get; set; }

        // ── Calculated from contract ───────────────────────────────────────────
        public decimal InsurancePayment { get; set; }   // reimbursement from formula
        public decimal PatientPay { get; set; }   // contract.ExpectedPatientPay
        public decimal NetMargin { get; set; }   // InsurancePayment + PatientPay − ACQ

        // ── Flags ──────────────────────────────────────────────────────────────
        public bool IsUnderwater { get; set; }   // NetMargin < 0
        public bool MissingSnapshot { get; set; }   // required price field (AWP/ASP/MAC) is null
    }
}
