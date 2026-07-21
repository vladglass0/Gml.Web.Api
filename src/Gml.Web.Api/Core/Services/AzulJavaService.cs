using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Gml.Web.Api.Core.Models.Java;
using Gml.Web.Api.Core.Options;

namespace Gml.Web.Api.Core.Services;

public class AzulJavaService(IHttpClientFactory httpClientFactory) : IAzulJavaService
{
    public async Task<IReadOnlyList<JavaVersionDto>> ListPackagesAsync(
        int majorVersion,
        string? os = null,
        string? arch = null,
        CancellationToken cancellationToken = default)
    {
        os ??= DetectOs();
        arch ??= DetectArch();

        var client = httpClientFactory.CreateClient(HttpClientNames.AzulMetadata);
        var url =
            $"zulu/packages/?java_version={majorVersion}" +
            $"&os={Uri.EscapeDataString(os)}" +
            $"&arch={Uri.EscapeDataString(arch)}" +
            "&java_package_type=jdk" +
            "&archive_type=zip" +
            "&javafx_bundled=false" +
            "&release_status=ga" +
            "&availability_types=CA" +
            "&latest=true" +
            "&page=1&page_size=20";

        using var response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var packages = await response.Content.ReadFromJsonAsync<List<AzulPackage>>(cancellationToken: cancellationToken)
                       ?? [];

        return packages
            .Where(p => !string.IsNullOrWhiteSpace(p.DownloadUrl))
            .Select(p => new JavaVersionDto
            {
                Name = string.IsNullOrWhiteSpace(p.Name) ? $"Zulu {majorVersion}" : Path.GetFileNameWithoutExtension(p.Name),
                Version = FormatJavaVersion(p.JavaVersion) ?? p.Name ?? majorVersion.ToString(),
                MajorVersion = p.JavaVersion is { Count: > 0 } ? p.JavaVersion[0] : majorVersion,
                Source = JavaRuntimeSource.Azul,
                DownloadUrl = p.DownloadUrl,
                PackageUuid = p.PackageUuid,
                Os = os,
                Arch = arch,
                Recommended = false
            })
            .GroupBy(p => p.PackageUuid ?? p.DownloadUrl)
            .Select(g => g.First())
            .ToList();
    }

    public static string DetectOs()
    {
        if (OperatingSystem.IsWindows()) return "windows";
        if (OperatingSystem.IsMacOS()) return "macos";
        return "linux";
    }

    public static string DetectArch()
    {
        // Azul Metadata API: x64 Windows/Linux often filtered as arch=x86 (returns win_x64 / linux_x64).
        return System.Runtime.InteropServices.RuntimeInformation.OSArchitecture switch
        {
            System.Runtime.InteropServices.Architecture.Arm64 => "arm",
            _ => "x86"
        };
    }

    private static string? FormatJavaVersion(List<int>? parts)
    {
        if (parts is null || parts.Count == 0) return null;
        return string.Join('.', parts);
    }

    private sealed class AzulPackage
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("download_url")]
        public string? DownloadUrl { get; set; }

        [JsonPropertyName("package_uuid")]
        public string? PackageUuid { get; set; }

        [JsonPropertyName("java_version")]
        public List<int>? JavaVersion { get; set; }
    }
}
