namespace Gml.Web.Api.Core.Models.FileManager;

public class FileManagerContentDto
{
    public string RelativePath { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string Encoding { get; set; } = "utf-8";
    public bool IsBinary { get; set; }
}
