namespace SearchTool_ServerSide.Dtos.BranchDTOs
{
    public class BranchWithUsersCountDto
    {
        public int BranchId { get; set; }
        public string BranchName { get; set; }
        public int UsersCount { get; set; }
    }
}