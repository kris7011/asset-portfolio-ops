namespace AssetPortfolioOps.Api.Domain;

public sealed class InventoryItem
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public int QuantityAvailable { get; set; }
    public string WarehouseLocation { get; set; } = string.Empty;
    public DateTime LastUpdatedUtc { get; set; }
}
