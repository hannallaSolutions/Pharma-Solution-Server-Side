using CsvHelper.Configuration.Attributes;
using ServerSide.Model;

namespace SearchTool_ServerSide.Models
{
    public class DrugDiseaseAddHistory : IEntity
    {
        public int Id { get; set; }
        public int DrugId { get; set; }
        public int DiseaseId { get; set; }
        public int UserId { get; set; } 
        public bool Show { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime EditedAt { get; set; }
        public Drug Drug { get; set; }
        public Disease Disease { get; set; }
        public User User { get; set; }
    }
}