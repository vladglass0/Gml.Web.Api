using Gml.Domains.Integrations;

namespace Gml.Web.Api.Core.Integrations.Auth;

/// <summary>
/// Auth result that can carry tokens issued by an external site (e.g. UnicoreCMS).
/// </summary>
public class ExtendedAuthResult : AuthResult
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
}
