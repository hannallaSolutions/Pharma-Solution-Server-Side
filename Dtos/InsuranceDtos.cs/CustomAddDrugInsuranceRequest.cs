namespace SearchTool_ServerSide.Dtos.InsuranceDtos.cs
{
    public class CustomAddDrugInsuranceRequest
    {
        public int DrugId { get; set; }
        public string InsuranceRx { get; set; }
        public string InsurancePCN { get; set; }
        public string InsuranceBin { get; set; }
        public string InsuranceBinCode { get; set; }
    }
}