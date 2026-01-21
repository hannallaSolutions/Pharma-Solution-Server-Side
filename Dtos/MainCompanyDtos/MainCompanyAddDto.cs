namespace SearchTool_ServerSide.Dtos.MainCompanyDtos
{
    public class MainCompanyAddDto
    {
        public string Name { get; set; } = string.Empty;
        public int SpecialtyId { get; set; }

    }

    public class CreateMainCompanyDto
{
    public string Name { get; set; }
    public int SpecialtyId { get; set; }
    public int? ClassTypeId { get; set; }
}

public class MainCompanyDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int SpecialtyId { get; set; }
    public string SpecialtyName { get; set; }
    public int? ClassTypeId { get; set; }
}

}