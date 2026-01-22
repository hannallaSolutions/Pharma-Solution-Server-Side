namespace SearchTool_ServerSide.Dtos.BranchDTOs
{
    /*
    public class CreateBranchDto
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int MainCompanyId { get; set; }
    }

    public class BranchDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int MainCompanyId { get; set; }
        public string MainCompanyName { get; set; } = string.Empty;
    }
*/
    public class CreateBranchDto
{
    public string Name { get; set; }
    public string Location { get; set; }
    public string Code { get; set; }
    public int MainCompanyId { get; set; }
}

public class BranchDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Location { get; set; }
    public string Code { get; set; }
    public int MainCompanyId { get; set; }
    public string MainCompanyName { get; set; }
}



}