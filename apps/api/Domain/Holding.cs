namespace AssetPortfolioOps.Api.Domain;

public sealed class Holding
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid AssetId { get; set; }
    public int Quantity { get; set; }
    public decimal AveragePurchasePrice { get; set; }
}
