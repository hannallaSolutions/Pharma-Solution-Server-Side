namespace SearchTool_ServerSide.Dtos.DiseaseDtos
{
    public class DrugDiseaseHistoryAddDto
    {
        public int DrugId { get; set; }
        public int DiseaseId { get; set; }
        public int UserId { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime EditedAt { get; set; } = DateTime.UtcNow;
        public bool Show { get; set; } = true;

    }
}