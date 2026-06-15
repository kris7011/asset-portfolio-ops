namespace AssetPortfolioOps.Api.Features.RiskIndicators;

public interface IRiskIndicatorService
{
    Task<IReadOnlyList<RiskIndicatorResponse>?> GetForCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);
}