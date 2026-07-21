namespace Gml.Web.Api.Core.Models.FileManager;

public class FileManagerWriteRequest
{
    public string Scope { get; set; } = FileManagerScope.Global;
    public string Path { get; set; } = string.Empty;
    public string? ProfileName { get; set; }
    public string Content { get; set; } = string.Empty;
}
