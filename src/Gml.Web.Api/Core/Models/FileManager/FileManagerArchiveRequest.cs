namespace Gml.Web.Api.Core.Models.FileManager;

public class FileManagerArchiveRequest
{
    public string Scope { get; set; } = FileManagerScope.Global;
    public string? ProfileName { get; set; }
    public List<string> Paths { get; set; } = [];
    public string ArchiveName { get; set; } = "archive.zip";
}
