using AssetPortfolioOps.Api.Domain;

namespace AssetPortfolioOps.Api.Features.PurchaseRequests;

public interface IPurchaseRequestService
{
    IReadOnlyCollection<PurchaseRequest> GetAll();
    Task<PurchaseRequest> CreateAsync(CreatePurchaseRequestRequest request);
}
