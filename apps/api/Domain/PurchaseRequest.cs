namespace AssetPortfolioOps.Api.Domain;

public sealed class PurchaseRequest
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid AssetId { get; set; }
    public int Quantity { get; set; }
    public decimal RequestedPrice { get; set; }
    public PurchaseRequestStatus Status { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
}
