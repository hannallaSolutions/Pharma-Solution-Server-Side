using ServerSide.Model;

namespace SearchTool_ServerSide.Models
{
    public class DrugDisease : IEntity
    {
        public int Id { get; set; }
        public int DrugId { get; set; }
        public int DiseaseId { get; set; }

        public Drug Drug { get; set; }
        public Disease Disease { get; set; }
    }
}