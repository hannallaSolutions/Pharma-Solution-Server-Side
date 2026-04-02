using SearchTool_ServerSide.Models;

namespace SearchTool_ServerSide.Dtos.DrugDtos
{
    public class SuperAdminDrugReadDto
    {
     public int Id { get; set; }
        public required string Name { get; set; }
        public required string NDC { get; set; }
        public string? Form { get; set; }
        public string? Strength { get; set; }
        public decimal ACQ { get; set; }
        public decimal AWP { get; set; }
        public decimal? Rxcui { get; set; }
        public string? Route { get; set; }
        public string? TECode { get; set; }
        public string? Ingrdient { get; set; }
        public string? ApplicationNumber { get; set; }
        public string? ApplicationType { get; set; }
        public string? StrengthUnit { get; set; }
        public string? Type { get; set; }
        public ICollection<ClassInfo> Classes { get; set; } = new List<ClassInfo>();
        public ICollection<InsuranceRx?> RxGroups { get; set; } = new List<InsuranceRx?>();
    }
}