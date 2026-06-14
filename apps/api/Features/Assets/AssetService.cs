using AssetPortfolioOps.Api.Data;
using AssetPortfolioOps.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AssetPortfolioOps.Api.Features.Assets;

public sealed class AssetService(AppDbContext dbContext) : IAssetService
{
    public IReadOnlyCollection<Asset> GetAll()
    {
        return dbContext.Assets
            .AsNoTracking()
            .OrderBy(asset => asset.Name)
            .ToList();
    }

    public Asset? GetById(Guid id)
    {
        return dbContext.Assets
            .AsNoTracking()
            .FirstOrDefault(asset => asset.Id == id);
    }
}
