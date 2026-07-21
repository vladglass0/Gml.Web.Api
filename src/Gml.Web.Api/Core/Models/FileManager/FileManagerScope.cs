namespace Gml.Web.Api.Core.Models.FileManager;

public static class FileManagerScope
{
    public const string Global = "global";
    public const string Profile = "profile";

    public static bool IsValid(string? scope) =>
        string.Equals(scope, Global, StringComparison.OrdinalIgnoreCase)
        || string.Equals(scope, Profile, StringComparison.OrdinalIgnoreCase);

    public static string RequiredPermission(string scope) =>
        string.Equals(scope, Profile, StringComparison.OrdinalIgnoreCase)
            ? "files.profile.manage"
            : "files.global.manage";
}
