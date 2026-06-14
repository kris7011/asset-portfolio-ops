namespace AssetPortfolioOps.Api.Features.Portfolios;

public sealed class PortfolioResponse
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public IReadOnlyCollection<PortfolioItemResponse> Items { get; set; } = Array.Empty<PortfolioItemResponse>();
    public decimal TotalMarketValue { get; set; }
    public decimal TotalGainLoss { get; set; }
}
