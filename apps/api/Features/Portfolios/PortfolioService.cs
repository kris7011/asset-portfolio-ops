using AssetPortfolioOps.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace AssetPortfolioOps.Api.Features.Portfolios;

public sealed class PortfolioService(AppDbContext dbContext) : IPortfolioService
{
    public PortfolioResponse? GetPortfolio(Guid customerId)
    {
        var customer = dbContext.Customers
            .AsNoTracking()
            .FirstOrDefault(customer => customer.Id == customerId);

        if (customer is null)
        {
            return null;
        }

        var holdingsWithAssets = dbContext.Holdings
            .AsNoTracking()
            .Where(holding => holding.CustomerId == customerId)
            .Join(
                dbContext.Assets.AsNoTracking(),
                holding => holding.AssetId,
                asset => asset.Id,
                (holding, asset) => new
                {
                    Holding = holding,
                    Asset = asset
                })
            .ToList();

        var items = holdingsWithAssets
            .Select(item =>
            {
                var totalMarketValue = item.Holding.Quantity * item.Asset.MarketPrice;
                var totalPurchaseValue = item.Holding.Quantity * item.Holding.AveragePurchasePrice;

                return new PortfolioItemResponse
                {
                    AssetId = item.Asset.Id,
                    AssetName = item.Asset.Name,
                    Region = item.Asset.Region,
                    VintageYear = item.Asset.VintageYear,
                    Quantity = item.Holding.Quantity,
                    AveragePurchasePrice = item.Holding.AveragePurchasePrice,
                    MarketPrice = item.Asset.MarketPrice,
                    TotalMarketValue = totalMarketValue,
                    GainLoss = totalMarketValue - totalPurchaseValue
                };
            })
            .OrderByDescending(item => item.TotalMarketValue)
            .ToList();

        return new PortfolioResponse
        {
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            Items = items,
            TotalMarketValue = items.Sum(item => item.TotalMarketValue),
            TotalGainLoss = items.Sum(item => item.GainLoss)
        };
    }
}
