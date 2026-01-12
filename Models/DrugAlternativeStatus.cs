namespace SearchTool_ServerSide.Models
{
    public class DrugAlternativeStatus
    {
        public string SourceDrugNDC { get; set; }
        public string TargetDrugNDC { get; set; }
        public Drug SourceDrug { get; set; }
        public Drug TargetDrug { get; set; }
        public int ClassInfoId { get; set; }
        public ClassInfo ClassInfo { get; set; }
        public string ApprovedStatus { get; set; } = "NA";
        public ICollection<DrugAlternativeReport> Reports { get; set; } = new List<DrugAlternativeReport>();

    }
    public class DrugAlternativeReport
    {
        public int Id { get; set; }  // simple PK for the history row

        // Composite FK back to DrugAlternativeStatus
        public string SourceDrugNDC { get; set; }
        public string TargetDrugNDC { get; set; }
        public int ClassInfoId { get; set; }
        public DrugAlternativeStatus DrugAlternativeStatus { get; set; }

        public string Status { get; set; } = "Approved";
        public string StatusDescription { get; set; } = "The drug is approved for use.";
        public string AdditionalInfo { get; set; } = "No additional information available.";
        public DateTime StatusDate { get; set; } = DateTime.UtcNow;

        public string UserEmail { get; set; }
        public User User { get; set; }
    }
}