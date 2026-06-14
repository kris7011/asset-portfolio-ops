namespace AssetPortfolioOps.Api.Domain;

public sealed class Asset
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public AssetType Type { get; set; }
    public string Region { get; set; } = string.Empty;
    public int VintageYear { get; set; }
    public decimal MarketPrice { get; set; }
    public string Currency { get; set; } = "DKK";
}
