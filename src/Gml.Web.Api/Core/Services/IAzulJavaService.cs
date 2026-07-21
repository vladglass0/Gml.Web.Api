using Gml.Web.Api.Core.Models.Java;

namespace Gml.Web.Api.Core.Services;

public interface IAzulJavaService
{
    Task<IReadOnlyList<JavaVersionDto>> ListPackagesAsync(
        int majorVersion,
        string? os = null,
        string? arch = null,
        CancellationToken cancellationToken = default);
}
