using AssetPortfolioOps.Api.Data;

namespace AssetPortfolioOps.Api.Features.Portfolios;

public sealed class PortfolioService(InMemoryDataStore store) : IPortfolioService
{
    public PortfolioResponse? GetPortfolio(Guid customerId)
    {
        var customer = store.Customers.FirstOrDefault(customer => customer.Id == customerId);

        if (customer is null)
        {
            return null;
        }

        var items = store.Holdings
            .Where(holding => holding.CustomerId == customerId)
            .Join(
                store.Assets,
                holding => holding.AssetId,
                asset => asset.Id,
                (holding, asset) =>
                {
                    var totalMarketValue = holding.Quantity * asset.MarketPrice;
                    var totalPurchaseValue = holding.Quantity * holding.AveragePurchasePrice;

                    return new PortfolioItemResponse
                    {
                        AssetId = asset.Id,
                        AssetName = asset.Name,
                        Region = asset.Region,
                        VintageYear = asset.VintageYear,
                        Quantity = holding.Quantity,
                        AveragePurchasePrice = holding.AveragePurchasePrice,
                        MarketPrice = asset.MarketPrice,
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
