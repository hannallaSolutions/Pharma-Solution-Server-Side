namespace SearchTool_ServerSide.Dtos.DashboardDto
{
    public class ScriptAnalyticsDto
    {
        // Identity
        public long? ScriptId { get; set; }
        public string? ScriptCode { get; set; }
        public string? RxNumber { get; set; }
        public DateTime? Date { get; set; }

        // Drug
        public long? DrugId { get; set; }
        public string? DrugName { get; set; }
        public string? NdcCode { get; set; }
        public string? DrugClass { get; set; }

        // Insurance
        public long? InsuranceId { get; set; }
        public string? InsuranceName { get; set; }
        public string? InsuranceRx { get; set; }
        public string? BinCode { get; set; }
        public string? PcnCode { get; set; }

        // Prescriber
        public long? PrescriberId { get; set; }
        public string? PrescriberName { get; set; }

        // Branch
        public long? BranchId { get; set; }
        public string? BranchCode { get; set; }
        public string? BranchName { get; set; }

        // User (dispensing tech / pharmacist)
        public long? UserId { get; set; }
        public string? UserName { get; set; }

        // Financial (raw values from ScriptItem)
        public decimal Quantity { get; set; }
        public decimal InsurancePayment { get; set; }
        public decimal PatientPayment { get; set; }
        public decimal AcquisitionCost { get; set; }

        // Financial (calculated in projection)
        public decimal TotalRevenue { get; set; }
        public decimal NetProfit { get; set; }
        public decimal? NetProfitPerItem { get; set; }

        // Imported net profit — the original "NP" value from the source
        // import (ScriptItem.OriginalNetProfit), kept separate from the
        // calculated NetProfit above. Does not replace or affect NetProfit.
        public decimal? OriginalNetProfit { get; set; }

        // Workflow
        public string? Status { get; set; }
        public string? RxStatus { get; set; }
        public string? Priority { get; set; }

        // Nullable — populated in a later sprint when Medisearch is integrated
        public string? HighestDrugNdc { get; set; }
        public string? HighestDrugName { get; set; }
        public decimal? HighestNet { get; set; }
        public decimal? Difference { get; set; }
    }
}
