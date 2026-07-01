namespace SearchTool_ServerSide.Dtos.UserDtos
{
    public class UserBranchReadDto
    {
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public string? BranchCode { get; set; }
        public int MainCompanyId { get; set; }
        public string MainCompanyName { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; }
    }
}
