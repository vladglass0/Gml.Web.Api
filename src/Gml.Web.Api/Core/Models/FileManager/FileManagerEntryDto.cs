namespace Gml.Web.Api.Core.Models.FileManager;

public class FileManagerEntryDto
{
    public string Name { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public bool IsDirectory { get; set; }
    public long Size { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }
    public string? Extension { get; set; }
}
