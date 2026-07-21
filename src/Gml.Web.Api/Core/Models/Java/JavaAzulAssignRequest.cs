namespace Gml.Web.Api.Core.Models.Java;

public class JavaAzulAssignRequest
{
    public string? PackageUuid { get; set; }
    public string? DownloadUrl { get; set; }
    public string? Name { get; set; }
    public string? Version { get; set; }
    public int MajorVersion { get; set; }
    public string? Os { get; set; }
    public string? Arch { get; set; }
}
