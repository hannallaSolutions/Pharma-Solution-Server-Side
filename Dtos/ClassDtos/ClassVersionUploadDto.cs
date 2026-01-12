namespace SearchTool_ServerSide.Dtos.ClassDtos
{

    public class ClassVersionUploadDto
    {
        public IFormFile UploadedFile { get; set; }

        public string Name { get; set; }

        public string Description { get; set; } = "";

        public bool IsMultiple { get; set; }
    }

}