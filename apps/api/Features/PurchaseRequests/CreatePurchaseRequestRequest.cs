namespace AssetPortfolioOps.Api.Features.PurchaseRequests;

public sealed class CreatePurchaseRequestRequest
{
    public Guid CustomerId { get; set; }
    public Guid AssetId { get; set; }
    public int Quantity { get; set; }
    public decimal RequestedPrice { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
}
