using System.Collections.Concurrent;

namespace Gml.Web.Api.Core.Services;

/// <summary>
/// In-memory store for external (site) refresh tokens keyed by player UUID.
/// </summary>
public class ExternalPlayerTokenStore
{
    private readonly ConcurrentDictionary<string, string> _refreshByUuid =
        new(StringComparer.OrdinalIgnoreCase);

    public void SetRefreshToken(string uuid, string? refreshToken)
    {
        if (string.IsNullOrWhiteSpace(uuid))
            return;

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            _refreshByUuid.TryRemove(uuid, out _);
            return;
        }

        _refreshByUuid[uuid] = refreshToken;
    }

    public string? GetRefreshToken(string uuid)
    {
        if (string.IsNullOrWhiteSpace(uuid))
            return null;

        return _refreshByUuid.TryGetValue(uuid, out var token) ? token : null;
    }
}
