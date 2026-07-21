using System.Globalization;
using System.Text.RegularExpressions;
using Gml.Web.Api.Core.Models.Java;

namespace Gml.Web.Api.Core.Services;

public partial class JavaRecommendationService : IJavaRecommendationService
{
    public JavaRecommendDto Recommend(string? minecraftVersion)
    {
        var major = RecommendMajor(minecraftVersion);
        return new JavaRecommendDto
        {
            MinecraftVersion = minecraftVersion ?? string.Empty,
            MajorVersion = major,
            Label = $"Java {major}"
        };
    }

    public int RecommendMajor(string? minecraftVersion)
    {
        if (string.IsNullOrWhiteSpace(minecraftVersion))
            return 21;

        var match = VersionRegex().Match(minecraftVersion.Trim());
        if (!match.Success)
            return 21;

        var major = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var minor = match.Groups[2].Success
            ? int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture)
            : 0;
        var patch = match.Groups[3].Success
            ? int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture)
            : 0;

        // Mojang requirements:
        // <= 1.16.5 -> 8
        // 1.17 .. 1.20.4 -> 17
        // >= 1.20.5 -> 21
        if (major < 1 || (major == 1 && minor < 17))
            return 8;

        if (major == 1 && (minor < 20 || (minor == 20 && patch <= 4)))
            return 17;

        return 21;
    }

    [GeneratedRegex(@"^(\d+)(?:\.(\d+))?(?:\.(\d+))?")]
    private static partial Regex VersionRegex();
}
