namespace Gml.Web.Api.Core.Models.Java;

public class ProfileJavaMeta
{
    public string Source { get; set; } = JavaRuntimeSource.Default;
    public int JavaMajor { get; set; }
    public string? RuntimeId { get; set; }
    public string? JavaPath { get; set; }
    public string? Name { get; set; }
    public string? Version { get; set; }
    public string? PackageUuid { get; set; }
}
