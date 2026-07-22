namespace Gml.Web.Api.Core.Models.Unicore;

public class UnicorePlayerCabinetDto
{
    public bool Available { get; set; }
    public string? Message { get; set; }
    /// <summary>Unicore site balance (<c>real</c>), rubles.</summary>
    public decimal? Balance { get; set; }
    /// <summary>Base URL of UnicoreCMS (for cabinet link).</summary>
    public string? CabinetUrl { get; set; }
    public long TotalPlaytime { get; set; }
    public List<UnicorePlayerServerDto> Servers { get; set; } = [];
}

public class UnicorePlayerServerDto
{
    public string ServerId { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    public long Playtime { get; set; }
    public List<UnicoreDonateGroupDto> DonateGroups { get; set; } = [];
}

public class UnicoreDonateGroupDto
{
    public string Name { get; set; } = string.Empty;
    public string? IngameId { get; set; }
    public DateTime? Expired { get; set; }
}
