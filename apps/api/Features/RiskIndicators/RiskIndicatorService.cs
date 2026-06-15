using AssetPortfolioOps.Api.Data;
using AssetPortfolioOps.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AssetPortfolioOps.Api.Features.RiskIndicators;

public sealed class RiskIndicatorService(AppDbContext dbContext) : IRiskIndicatorService
{
    public async Task<IReadOnlyList<RiskIndicatorResponse>?> GetForCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var customerExists = await dbContext.Customers
            .AnyAsync(customer => customer.Id == customerId, cancellationToken);

        if (!customerExists)
        {
            return null;
        }

        var indicators = new List<RiskIndicatorResponse>();

        await AddConcentrationRiskAsync(customerId, indicators, cancellationToken);
        await AddLowInventoryWarningsAsync(customerId, indicators, cancellationToken);
        await AddPendingRequestExposureAsync(customerId, indicators, cancellationToken);

        if (indicators.Count == 0)
        {
            indicators.Add(new RiskIndicatorResponse(
                "NoRiskDetected",
                "Low",
                "No significant portfolio risks detected",
                "The portfolio does not currently show high concentration, low inventory or pending request exposure."));
        }

        return indicators;
    }

    private async Task AddConcentrationRiskAsync(
        Guid customerId,
        List<RiskIndicatorResponse> indicators,
        CancellationToken cancellationToken)
    {
        var holdings = await dbContext.Holdings
            .Where(holding => holding.CustomerId == customerId)
            .Join(
                dbContext.Assets,
                holding => holding.AssetId,
                asset => asset.Id,
                (holding, asset) => new
                {
                    AssetName = asset.Name,
                    MarketValue = holding.Quantity * asset.MarketPrice
                })
            .ToListAsync(cancellationToken);

        var totalMarketValue = holdings.Sum(holding => holding.MarketValue);

        if (totalMarketValue <= 0)
        {
            return;
        }

        var largestHolding = holdings
            .OrderByDescending(holding => holding.MarketValue)
            .First();

        var concentrationPercentage = largestHolding.MarketValue / totalMarketValue * 100;

        if (concentrationPercentage < 60)
        {
            return;
        }

        var severity = concentrationPercentage >= 70 ? "High" : "Medium";

        indicators.Add(new RiskIndicatorResponse(
            "HighConcentration",
            severity,
            "High portfolio concentration",
            $"{largestHolding.AssetName} represents {Math.Round(concentrationPercentage, 1)}% of the total portfolio value."));
    }

    private async Task AddLowInventoryWarningsAsync(
        Guid customerId,
        List<RiskIndicatorResponse> indicators,
        CancellationToken cancellationToken)
    {
        var customerAssetIds = await dbContext.Holdings
            .Where(holding => holding.CustomerId == customerId)
            .Select(holding => holding.AssetId)
            .ToListAsync(cancellationToken);

        var inventoryItems = await dbContext.InventoryItems
            .Where(item => customerAssetIds.Contains(item.AssetId))
            .Join(
                dbContext.Assets,
                inventoryItem => inventoryItem.AssetId,
                asset => asset.Id,
                (inventoryItem, asset) => new
                {
                    AssetName = asset.Name,
                    inventoryItem.QuantityAvailable
                })
            .ToListAsync(cancellationToken);

        foreach (var inventoryItem in inventoryItems.Where(item => item.QuantityAvailable <= 10))
        {
            var severity = inventoryItem.QuantityAvailable <= 5 ? "High" : "Medium";

            indicators.Add(new RiskIndicatorResponse(
                "LowInventory",
                severity,
                "Low inventory",
                $"{inventoryItem.AssetName} has only {inventoryItem.QuantityAvailable} units available."));
        }
    }

    private async Task AddPendingRequestExposureAsync(
        Guid customerId,
        List<RiskIndicatorResponse> indicators,
        CancellationToken cancellationToken)
    {
        var pendingRequests = await dbContext.PurchaseRequests
            .Where(request =>
                request.CustomerId == customerId &&
                request.Status == PurchaseRequestStatus.Pending)
            .ToListAsync(cancellationToken);

        if (pendingRequests.Count == 0)
        {
            return;
        }

        var pendingExposure = pendingRequests.Sum(request =>
            request.Quantity * request.RequestedPrice);

        var severity = pendingExposure >= 50000
            ? "High"
            : pendingExposure >= 25000
                ? "Medium"
                : "Low";

        indicators.Add(new RiskIndicatorResponse(
            "PendingRequestExposure",
            severity,
            "Pending request exposure",
            $"{pendingRequests.Count} pending purchase request(s) represent {Math.Round(pendingExposure, 0)} DKK in potential new exposure."));
    }
}