using AssetPortfolioOps.Api.Data;
using AssetPortfolioOps.Api.Domain;

namespace AssetPortfolioOps.Api.Features.Assets;

public sealed class AssetService(InMemoryDataStore store) : IAssetService
{
    public IReadOnlyCollection<Asset> GetAll()
    {
        return store.Assets
            .OrderBy(asset => asset.Name)
            .ToList();
    }

    public Asset? GetById(Guid id)
    {
        return store.Assets.FirstOrDefault(asset => asset.Id == id);
    }
}
