namespace SearchTool_ServerSide.Dtos.ScriptsDtos
{

public class ScriptAnalyticsDto
{
    public long ScriptId { get; set; }
    public string? ScriptCode { get; set; }
    public string? RxNumber { get; set; }
    public DateTime? Date { get; set; }

    public long? DrugId { get; set; }
    public string? DrugName { get; set; }
    public string? NdcCode { get; set; }
    public string? DrugClass { get; set; }

    public long? InsuranceId { get; set; }
    public string? InsuranceName { get; set; }
    public string? InsuranceRx { get; set; }
    public string? BinCode { get; set; }
    public string? PcnCode { get; set; }

    public long? PrescriberId { get; set; }
    public string? PrescriberName { get; set; }

    public long? BranchId { get; set; }
    public string? BranchCode { get; set; }
    public string? BranchName { get; set; }

    public long? UserId { get; set; }
    public string? UserName { get; set; }

    public decimal Quantity { get; set; }
    public decimal InsurancePayment { get; set; }
    public decimal PatientPayment { get; set; }
    public decimal AcquisitionCost { get; set; }

    public decimal TotalRevenue { get; set; }
    public decimal NetProfit { get; set; }
    public decimal? NetProfitPerItem { get; set; }

    public string? Status { get; set; }
    public string? RxStatus { get; set; }
    public string? Priority { get; set; }

    public string? HighestDrugNdc { get; set; }
    public string? HighestDrugName { get; set; }
    public decimal? HighestNet { get; set; }
    public decimal? Difference { get; set; }
}

}