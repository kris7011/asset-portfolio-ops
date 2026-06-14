using AssetPortfolioOps.Api.Domain;

namespace AssetPortfolioOps.Api.Features.Inventory;

public interface IInventoryService
{
    IReadOnlyCollection<InventoryItem> GetAll();
}
