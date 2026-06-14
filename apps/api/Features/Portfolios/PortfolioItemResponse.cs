namespace AssetPortfolioOps.Api.Features.Portfolios;

public sealed class PortfolioItemResponse
{
    public Guid AssetId { get; set; }
    public string AssetName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public int VintageYear { get; set; }
    public int Quantity { get; set; }
    public decimal AveragePurchasePrice { get; set; }
    public decimal MarketPrice { get; set; }
    public decimal TotalMarketValue { get; set; }
    public decimal GainLoss { get; set; }
}
