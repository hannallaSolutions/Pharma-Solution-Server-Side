namespace SearchTool_ServerSide.Dtos
{
    public class SimpleScriptDto
    {
        public int Id { get; set; }
        public string ScriptCode { get; set; }
        public DateTime Date { get; set; }

        public int BranchId { get; set; }
        public string BranchName { get; set; }

        public int ItemsCount { get; set; }
        public decimal TotalNetProfit { get; set; }

        public List<SimpleScriptItemDto> Items { get; set; } = new();
    }

    public class SimpleScriptItemDto
    {
        public int Id { get; set; }
        public string RxNumber { get; set; }
        public string PF { get; set; }
        public decimal Quantity { get; set; }

        public decimal AcquisitionCost { get; set; }
        public decimal Discount { get; set; }
        public decimal InsurancePayment { get; set; }
        public decimal PatientPayment { get; set; }
        public decimal NetProfit { get; set; }

        public string NDCCode { get; set; }

        public int DrugId { get; set; }
        public string DrugName { get; set; }

        public int InsuranceId { get; set; }
        public string InsuranceName { get; set; }

        public string UserEmail { get; set; }
        public string PrescriberName { get; set; }
    }

    public class PagedResponse<T>
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public List<T> Data { get; set; } = new();
    }
}
