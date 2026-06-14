using AssetPortfolioOps.Api.Data;
using AssetPortfolioOps.Api.Domain;

namespace AssetPortfolioOps.Api.Features.Inventory;

public sealed class InventoryService(InMemoryDataStore store) : IInventoryService
{
    public IReadOnlyCollection<InventoryItem> GetAll()
    {
        return store.InventoryItems
            .OrderBy(item => item.WarehouseLocation)
            .ToList();
    }
}
