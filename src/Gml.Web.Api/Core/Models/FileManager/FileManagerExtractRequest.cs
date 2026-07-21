namespace Gml.Web.Api.Core.Models.FileManager;

public class FileManagerExtractRequest
{
    public string Scope { get; set; } = FileManagerScope.Global;
    public string? ProfileName { get; set; }
    public string Path { get; set; } = string.Empty;
    public string? Destination { get; set; }
}
