using AssetPortfolioOps.Api.Data;
using AssetPortfolioOps.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AssetPortfolioOps.Api.Features.Inventory;

public sealed class InventoryService(AppDbContext dbContext) : IInventoryService
{
    public IReadOnlyCollection<InventoryItem> GetAll()
    {
        return dbContext.InventoryItems
            .AsNoTracking()
            .OrderBy(item => item.WarehouseLocation)
            .ToList();
    }
}
