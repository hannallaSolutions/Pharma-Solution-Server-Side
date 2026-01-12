namespace SearchTool_ServerSide.Models
{
    public class  UserPermission
    {
       public int UserId { get; set; }
       public User User { get; set; } = default! ; //this means it cannot be null and must be initialized 
       public int PermissionId { get; set; }
       public Permission Permission { get; set; } = default! ;

    }
}