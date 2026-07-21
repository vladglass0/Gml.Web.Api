namespace Gml.Web.Api.Core.Models.Java;

public class JavaVersionDto
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public int MajorVersion { get; set; }
    public string Source { get; set; } = JavaRuntimeSource.Azul;
    public string? DownloadUrl { get; set; }
    public string? PackageUuid { get; set; }
    public string? Os { get; set; }
    public string? Arch { get; set; }
    public bool Recommended { get; set; }
}

public static class JavaRuntimeSource
{
    public const string Default = "default";
    public const string Azul = "azul";
    public const string Upload = "upload";
}
