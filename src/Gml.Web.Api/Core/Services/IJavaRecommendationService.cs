using Gml.Web.Api.Core.Models.Java;

namespace Gml.Web.Api.Core.Services;

public interface IJavaRecommendationService
{
    JavaRecommendDto Recommend(string? minecraftVersion);
    int RecommendMajor(string? minecraftVersion);
}
