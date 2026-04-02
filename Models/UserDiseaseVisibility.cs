using ServerSide.Model;

namespace SearchTool_ServerSide.Models
{
    public class UserDiseaseVisibility : IEntity
    {
        public int Id { get; set; }  //  required by IEntity

        public int UserId { get; set; }
        public int DiseaseId { get; set; }

        public User User { get; set; }
        public Disease Disease { get; set; }
    }
}
