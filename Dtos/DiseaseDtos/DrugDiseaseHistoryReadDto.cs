namespace SearchTool_ServerSide.Dtos.DiseaseDtos
{
    public class DrugDiseaseHistoryReadDto
    {
        public int Id { get; set; }
        public string UserEmail { get; set; } = "";
        public string UserName { get; set; } = "";
        public string DrugName { get; set; } = "";
        public int DrugId { get; set; }
        public int DiseaseId { get; set; }
        public string DiseaseName { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime EditedAt { get; set; }
        public bool Show { get; set; }
    }
}