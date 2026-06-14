using AssetPortfolioOps.Api.Domain;

namespace AssetPortfolioOps.Api.Data;

public sealed class InMemoryDataStore
{
    public List<Customer> Customers { get; } = SeedData.Customers.ToList();
    public List<Asset> Assets { get; } = SeedData.Assets.ToList();
    public List<Holding> Holdings { get; } = SeedData.Holdings.ToList();
    public List<InventoryItem> InventoryItems { get; } = SeedData.InventoryItems.ToList();
    public List<PurchaseRequest> PurchaseRequests { get; } = new();
}
