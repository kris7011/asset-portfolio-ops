using AssetPortfolioOps.Api.Domain;

namespace AssetPortfolioOps.Api.Features.Assets;

public interface IAssetService
{
    IReadOnlyCollection<Asset> GetAll();
    Asset? GetById(Guid id);
}
