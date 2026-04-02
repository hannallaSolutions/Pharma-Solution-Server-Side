namespace SearchTool_ServerSide.Models
{
    public class  UserPermission
    {
       public int UserId { get; set; } // foreign key
       public User User { get; set; } = default! ; //this means it cannot be null and must be initialized 
       public int PermissionId { get; set; } // foreign key
       public Permission Permission { get; set; } = default! ; // it is navigation property

    }
}