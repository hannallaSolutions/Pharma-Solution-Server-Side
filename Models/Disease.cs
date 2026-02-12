namespace SearchTool_ServerSide.Models
{
    public class Disease : ServerSide.Model.IEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool Show { get; set; }

     

    }
}