using ServerSide.Model;

namespace SearchTool_ServerSide.Models
{
    public class DrugDisease : IEntity
    {
        public int Id { get; set; }
        public int DrugId { get; set; }
        public int DiseaseId { get; set; }
        public string userEmail { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool Show { get; set; }
        public User User { get; set; }
        public Drug Drug { get; set; }
        public Disease Disease { get; set; }
    }
}