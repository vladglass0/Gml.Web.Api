using System.Formats.Tar;
using System.IO.Compression;
using System.Text.Json;
using Gml.Web.Api.Core.Models.Java;
using GmlCore.Interfaces;
using GmlCore.Interfaces.Launcher;

namespace Gml.Web.Api.Core.Services;

public interface IJavaRuntimeService
{
    Task<ProfileJavaMeta> GetMetaAsync(string profileName);
    Task<ProfileJavaMeta> SetDefaultAsync(string profileName);
    Task<ProfileJavaMeta> AssignAzulAsync(string profileName, JavaAzulAssignRequest request, CancellationToken ct = default);
    Task<ProfileJavaMeta> AssignUploadAsync(string profileName, Stream archiveStream, string fileName, CancellationToken ct = default);
    Task ApplyToProfileAsync(IGameProfile profile, ProfileJavaMeta meta);
}

public class JavaRuntimeService(
    IGmlManager gmlManager,
    IHttpClientFactory httpClientFactory) : IJavaRuntimeService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<ProfileJavaMeta> GetMetaAsync(string profileName)
    {
        var path = GetMetaPath(profileName);
        if (!File.Exists(path))
        {
            return new ProfileJavaMeta { Source = JavaRuntimeSource.Default };
        }

        await using var stream = File.OpenRead(path);
        var meta = await JsonSerializer.DeserializeAsync<ProfileJavaMeta>(stream, JsonOptions);
        return meta ?? new ProfileJavaMeta { Source = JavaRuntimeSource.Default };
    }

    public async Task<ProfileJavaMeta> SetDefaultAsync(string profileName)
    {
        _ = await RequireProfileAsync(profileName);
        var meta = new ProfileJavaMeta { Source = JavaRuntimeSource.Default };
        await SaveMetaAsync(profileName, meta);
        return meta;
    }

    public async Task<ProfileJavaMeta> AssignAzulAsync(
        string profileName,
        JavaAzulAssignRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.DownloadUrl) && string.IsNullOrWhiteSpace(request.PackageUuid))
            throw new ArgumentException("Укажите downloadUrl или packageUuid");

        var downloadUrl = request.DownloadUrl;
        if (string.IsNullOrWhiteSpace(downloadUrl))
            throw new ArgumentException("downloadUrl обязателен для загрузки Azul JDK");

        var runtimeId = Guid.NewGuid().ToString("N");
        var runtimeDir = GetRuntimeDirectory(runtimeId);
        Directory.CreateDirectory(runtimeDir);

        var archivePath = Path.Combine(runtimeDir, "jdk.zip");
        var client = httpClientFactory.CreateClient();
        await using (var remote = await client.GetStreamAsync(downloadUrl, ct))
        await using (var file = File.Create(archivePath))
        {
            await remote.CopyToAsync(file, ct);
        }

        var extractDir = Path.Combine(runtimeDir, "jdk");
        Directory.CreateDirectory(extractDir);
        await ExtractArchiveAsync(archivePath, extractDir, ct);
        TryDelete(archivePath);

        var javaHome = FindJavaHome(extractDir)
                       ?? throw new InvalidOperationException("В архиве Azul не найден исполняемый файл java");

        var meta = new ProfileJavaMeta
        {
            Source = JavaRuntimeSource.Azul,
            JavaMajor = request.MajorVersion,
            RuntimeId = runtimeId,
            JavaPath = ToRelativeRuntimePath(javaHome),
            Name = request.Name ?? Path.GetFileNameWithoutExtension(downloadUrl),
            Version = request.Version,
            PackageUuid = request.PackageUuid
        };

        var profile = await RequireProfileAsync(profileName);
        await ApplyToProfileAsync(profile, meta);
        await SaveMetaAsync(profileName, meta);
        return meta;
    }

    public async Task<ProfileJavaMeta> AssignUploadAsync(
        string profileName,
        Stream archiveStream,
        string fileName,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("Не указано имя файла");

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (ext is not (".zip" or ".gz" or ".tgz"))
        {
            // allow .tar.gz
            if (!fileName.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Поддерживаются только .zip и .tar.gz");
        }

        var runtimeId = Guid.NewGuid().ToString("N");
        var runtimeDir = GetRuntimeDirectory(runtimeId);
        Directory.CreateDirectory(runtimeDir);

        var archivePath = Path.Combine(runtimeDir, SanitizeFileName(fileName));
        await using (var file = File.Create(archivePath))
        {
            await archiveStream.CopyToAsync(file, ct);
        }

        var extractDir = Path.Combine(runtimeDir, "jdk");
        Directory.CreateDirectory(extractDir);
        await ExtractArchiveAsync(archivePath, extractDir, ct);
        TryDelete(archivePath);

        var javaHome = FindJavaHome(extractDir)
                       ?? throw new InvalidOperationException("В архиве не найден исполняемый файл java");

        var meta = new ProfileJavaMeta
        {
            Source = JavaRuntimeSource.Upload,
            RuntimeId = runtimeId,
            JavaPath = ToRelativeRuntimePath(javaHome),
            Name = Path.GetFileNameWithoutExtension(fileName.Replace(".tar.gz", "", StringComparison.OrdinalIgnoreCase)),
            Version = "custom"
        };

        var profile = await RequireProfileAsync(profileName);
        await ApplyToProfileAsync(profile, meta);
        await SaveMetaAsync(profileName, meta);
        return meta;
    }

    public Task ApplyToProfileAsync(IGameProfile profile, ProfileJavaMeta meta)
    {
        // Gml.Core IGameProfile in current package has no JavaPath setter.
        // Runtime selection is persisted in runtimes/profiles/{name}.json sidecar.
        _ = profile;
        _ = meta;
        return Task.CompletedTask;
    }

    private async Task<IGameProfile> RequireProfileAsync(string profileName)
    {
        var profile = await gmlManager.Profiles.GetProfile(profileName);
        if (profile is null)
            throw new DirectoryNotFoundException($"Профиль \"{profileName}\" не найден");
        return profile;
    }

    private string GetRuntimesRoot()
    {
        var root = Path.Combine(gmlManager.LauncherInfo.InstallationDirectory, "runtimes");
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "profiles"));
        return root;
    }

    private string GetRuntimeDirectory(string runtimeId) =>
        Path.Combine(GetRuntimesRoot(), runtimeId);

    private string GetMetaPath(string profileName)
    {
        var safe = string.Join("_", profileName.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(GetRuntimesRoot(), "profiles", $"{safe}.json");
    }

    private async Task SaveMetaAsync(string profileName, ProfileJavaMeta meta)
    {
        var path = GetMetaPath(profileName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, meta, JsonOptions);
    }

    private string ToRelativeRuntimePath(string absoluteJavaHome)
    {
        var root = Path.GetFullPath(gmlManager.LauncherInfo.InstallationDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        var full = Path.GetFullPath(absoluteJavaHome);
        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            return full.Replace('\\', '/');
        return Path.GetRelativePath(gmlManager.LauncherInfo.InstallationDirectory, full).Replace('\\', '/');
    }

    private static string? FindJavaHome(string extractRoot)
    {
        var javaNames = OperatingSystem.IsWindows()
            ? new[] { "java.exe" }
            : new[] { "java" };

        foreach (var javaName in javaNames)
        {
            var matches = Directory.EnumerateFiles(extractRoot, javaName, SearchOption.AllDirectories)
                .Where(p =>
                {
                    var dir = Path.GetDirectoryName(p) ?? string.Empty;
                    return dir.EndsWith($"{Path.DirectorySeparatorChar}bin", StringComparison.OrdinalIgnoreCase)
                           || dir.EndsWith("/bin", StringComparison.OrdinalIgnoreCase)
                           || dir.EndsWith("\\bin", StringComparison.OrdinalIgnoreCase);
                })
                .ToList();

            if (matches.Count == 0)
                continue;

            var binDir = Path.GetDirectoryName(matches[0])!;
            return Path.GetDirectoryName(binDir); // JAVA_HOME
        }

        return null;
    }

    private static async Task ExtractArchiveAsync(string archivePath, string destination, CancellationToken ct)
    {
        if (archivePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            ZipFile.ExtractToDirectory(archivePath, destination, overwriteFiles: true);
            return;
        }

        if (archivePath.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase)
            || archivePath.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase))
        {
            await using var file = File.OpenRead(archivePath);
            await using var gzip = new GZipStream(file, CompressionMode.Decompress);
            await TarFile.ExtractToDirectoryAsync(gzip, destination, overwriteFiles: true, cancellationToken: ct);
            return;
        }

        if (archivePath.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
        {
            // treat as tar.gz fallback
            await using var file = File.OpenRead(archivePath);
            await using var gzip = new GZipStream(file, CompressionMode.Decompress);
            await TarFile.ExtractToDirectoryAsync(gzip, destination, overwriteFiles: true, cancellationToken: ct);
            return;
        }

        throw new InvalidOperationException("Неподдерживаемый формат архива");
    }

    private static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // ignore cleanup errors
        }
    }
}
