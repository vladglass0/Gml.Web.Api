namespace Gml.Web.Api.Core.Models.FileManager;

public class FileManagerDeleteRequest
{
    public string Scope { get; set; } = FileManagerScope.Global;
    public string? ProfileName { get; set; }
    public List<string> Paths { get; set; } = [];
}
