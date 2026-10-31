using AssetPortfolioOps.Api.Data;
using AssetPortfolioOps.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AssetPortfolioOps.Api.Tests;

internal static class TestDbContextFactory
{
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var dbContext = new AppDbContext(options);

        dbContext.Customers.AddRange(CloneCustomers());
        dbContext.Assets.AddRange(CloneAssets());
        dbContext.Holdings.AddRange(CloneHoldings());
        dbContext.InventoryItems.AddRange(CloneInventoryItems());
        dbContext.AssetValuations.AddRange(CloneValuations());
        dbContext.SaveChanges();

        return dbContext;
    }

    private static IEnumerable<Customer> CloneCustomers()
    {
        return SeedData.Customers.Select(customer => new Customer
        {
            Id = customer.Id,
            Name = customer.Name,
            Email = customer.Email
        });
    }

    private static IEnumerable<Asset> CloneAssets()
    {
        return SeedData.Assets.Select(asset => new Asset
        {
            Id = asset.Id,
            Name = asset.Name,
            Type = asset.Type,
            Region = asset.Region,
            VintageYear = asset.VintageYear,
            MarketPrice = asset.MarketPrice,
            Currency = asset.Currency
        });
    }

    private static IEnumerable<Holding> CloneHoldings()
    {
        return SeedData.Holdings.Select(holding => new Holding
        {
            Id = holding.Id,
            CustomerId = holding.CustomerId,
            AssetId = holding.AssetId,
            Quantity = holding.Quantity,
            AveragePurchasePrice = holding.AveragePurchasePrice
        });
    }

    private static IEnumerable<InventoryItem> CloneInventoryItems()
    {
        return SeedData.InventoryItems.Select(item => new InventoryItem
        {
            Id = item.Id,
            AssetId = item.AssetId,
            QuantityAvailable = item.QuantityAvailable,
            WarehouseLocation = item.WarehouseLocation,
            LastUpdatedUtc = item.LastUpdatedUtc
        });
    }

    private static IEnumerable<AssetValuation> CloneValuations()
    {
        return ValuationSeedData.Valuations.Select(valuation => new AssetValuation
        {
            Id = valuation.Id,
            AssetId = valuation.AssetId,
            ValuationDate = valuation.ValuationDate,
            Price = valuation.Price,
            Source = valuation.Source,
            IsDemonstrationData = valuation.IsDemonstrationData
        });
    }
}
