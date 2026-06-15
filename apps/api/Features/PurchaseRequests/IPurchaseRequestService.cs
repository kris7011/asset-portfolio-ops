using AssetPortfolioOps.Api.Domain;

namespace AssetPortfolioOps.Api.Features.PurchaseRequests;

public interface IPurchaseRequestService
{
    Task<IReadOnlyList<PurchaseRequest>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<PurchaseRequest> CreateAsync(
        CreatePurchaseRequestRequest request,
        CancellationToken cancellationToken = default);

    Task<PurchaseRequest> ApproveAsync(
        Guid id,
        string performedBy,
        CancellationToken cancellationToken = default);

    Task<PurchaseRequest> RejectAsync(
        Guid id,
        string performedBy,
        CancellationToken cancellationToken = default);

    Task<PurchaseRequest> CompleteAsync(
        Guid id,
        string performedBy,
        CancellationToken cancellationToken = default);
}