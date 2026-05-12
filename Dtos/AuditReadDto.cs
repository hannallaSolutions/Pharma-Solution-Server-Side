namespace SearchTool_ServerSide.Dtos
{
    public class AuditReadDto
    {
        // ── Current Script Info ──
        public DateTime Date { get; set; }
        public string ScriptCode { get; set; } = "";
        public string RxNumber { get; set; } = "";
        public string BranchCode { get; set; } = "";
        public string BranchName { get; set; } = "";

        // ── Current Drug Info ──
        public int DrugId { get; set; }
        public string DrugName { get; set; } = "";
        public string NDCCode { get; set; } = "";
        public string DrugClass { get; set; } = "";

        // ── Current Insurance Info ──
        public int InsuranceId { get; set; }
        public int RxGroupId { get; set; }
        public int PcnId { get; set; }
        public int BinId { get; set; }

        public string InsuranceRx { get; set; } = "";
        public string BINCode { get; set; } = "";
        public string BINName { get; set; } = "";
        public string PCNName { get; set; } = "";

        // ── User / Prescriber ──
        public string User { get; set; } = "";
        public string Prescriber { get; set; } = "";

        // ── Current Financial Values ──
        public string PF { get; set; } = "";
        public decimal Quantity { get; set; }
        public int RemainingStock { get; set; }

        public decimal AcquisitionCost { get; set; }
        public decimal Discount { get; set; }
        public decimal InsurancePayment { get; set; }
        public decimal PatientPayment { get; set; }

        public decimal NetProfit { get; set; }
        public decimal NetProfitPerItem { get; set; }

        // ── Original Source-System Profit Fields ──
        public decimal? OriginalNetProfit { get; set; }
        public decimal? NPDiscrepancy { get; set; }
        public decimal? GrossProfit { get; set; }

        // ── Pricing Reference Fields ──
        public decimal? AWP { get; set; }
        public decimal? WAC { get; set; }
        public decimal? SDRA { get; set; }
        public decimal? ReimbursementRatePctOfAWP { get; set; }

        // ── Supply / Refill Fields ──
        public int? Refill { get; set; }
        public int? DaySupply { get; set; }
        public DateTime? DaySupplyEndDate { get; set; }
        public DateTime? RefillDate { get; set; }
        public string? Unit { get; set; }

        // ── Status Fields ──
        public string? Status { get; set; }
        public string? RxStatus { get; set; }
        public string? Priority { get; set; }

        // ── Highest Alternative Drug Info ──
        public int? HighestDrugId { get; set; }
        public string HighestDrugName { get; set; } = "";
        public string HighestDrugNDC { get; set; } = "";

        // ── Highest Alternative Script Info ──
        public string HighestScriptCode { get; set; } = "";
        public DateTime? HighestScriptDate { get; set; }
        public decimal HighestQuantity { get; set; }
        public int HighestRemainingStock { get; set; }

        // ── Highest Alternative Insurance Info ──
        public int HighestRxGroupId { get; set; }
        public int HighestPcnId { get; set; }
        public int HighestBinId { get; set; }

        public string HighestInsuranceRx { get; set; } = "";
        public string HighestBINCode { get; set; } = "";
        public string HighestBINName { get; set; } = "";
        public string HighestPCNName { get; set; } = "";

        // ── Highest Alternative Financial Values ──
        public decimal HighestNet { get; set; }
        public decimal HighestNetProfitPerItem { get; set; }

        // ── Difference / Opportunity Fields ──
        public decimal Diff => HighestNet - NetProfit;

        public decimal DiffPerItem =>
            HighestNetProfitPerItem - NetProfitPerItem;
        public decimal? OriginalNetProfitPerItem { get; set; }

        public decimal? NPDiscrepancyPerItem { get; set; }
        public string NPComparisonStatus { get; set; } = "";

        public decimal Difference => HighestNet - NetProfit;
        public decimal DifferencePerItem => HighestNetProfitPerItem - NetProfitPerItem;

    
    }
}
