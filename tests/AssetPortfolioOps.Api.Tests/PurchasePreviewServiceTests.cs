using AssetPortfolioOps.Api.Data;
using AssetPortfolioOps.Api.Domain;
using AssetPortfolioOps.Api.Features.PurchasePreviews;

namespace AssetPortfolioOps.Api.Tests;

public sealed class PurchasePreviewServiceTests
{
    [Fact]
    public async Task Preview_ShowsTheConcentrationBeforeAndAfterThePurchase()
    {
        using var dbContext = TestDbContextFactory.Create();
        var service = new PurchasePreviewService(dbContext);

        var preview = await service.PreviewAsync(SeedData.CustomerEmmaId, SeedData.BordeauxAssetId, 2, null);

        Assert.NotNull(preview);
        Assert.True(preview.IsPreview);
        Assert.Equal(PurchasePreviewService.PreviewNotice, preview.Notice);

        // Before: 75,000 / 101,700. After: 100,000 / 126,700.
        Assert.Equal(101_700m, preview.Before.TotalMarketValue);
        Assert.Equal(73.75m, preview.Before.LargestSharePercent);
        Assert.Equal("High", preview.Before.Level);
        Assert.Equal(126_700m, preview.After.TotalMarketValue);
        Assert.Equal(78.93m, preview.After.LargestSharePercent);
        Assert.Equal("High", preview.After.Level);
        Assert.Equal(5.18m, preview.LargestShareChangePercentagePoints);

        var bordeauxAfter = Assert.Single(preview.After.Positions, position => position.AssetId == SeedData.BordeauxAssetId);
        Assert.Equal(8, bordeauxAfter.Quantity);

        Assert.Equal("Market", preview.PriceSource);
        Assert.Equal(25_000m, preview.EstimatedCost);
        Assert.True(preview.Inventory.IsSufficient);
        Assert.Contains(preview.Warnings, warning => warning.Code == "ConcentrationRemains" && warning.Severity == "High");
    }

    [Fact]
    public async Task Preview_ReportsADecreaseWhenThePurchaseDiversifies()
    {
        using var dbContext = TestDbContextFactory.Create();

        var preview = await new PurchasePreviewService(dbContext)
            .PreviewAsync(SeedData.CustomerEmmaId, SeedData.BurgundyAssetId, 2, null);

        Assert.NotNull(preview);

        // Burgundy 5 x 8,900 = 44,500; Bordeaux 75,000 / 119,500.
        Assert.Equal(62.76m, preview.After.LargestSharePercent);
        Assert.Equal("Medium", preview.After.Level);
        Assert.Equal(-10.98m, preview.LargestShareChangePercentagePoints);

        var warning = Assert.Single(preview.Warnings, item => item.Code.StartsWith("Concentration"));
        Assert.Equal("ConcentrationDecrease", warning.Code);
        Assert.Contains("from High to Medium", warning.Message);
    }

    [Fact]
    public async Task Preview_WarnsWhenThePurchaseRaisesTheConcentrationLevel()
    {
        using var dbContext = TestDbContextFactory.Create();

        // A balanced portfolio: 2 x 12,500 Bordeaux and 6 x 4,200 Champagne (49.8% / 50.2%).
        var customerId = Guid.NewGuid();
        dbContext.Customers.Add(new Customer { Id = customerId, Name = "Balanced", Email = "b@example.com" });
        dbContext.Holdings.AddRange(
            new Holding { Id = Guid.NewGuid(), CustomerId = customerId, AssetId = SeedData.BordeauxAssetId, Quantity = 2, AveragePurchasePrice = 12_000m },
            new Holding { Id = Guid.NewGuid(), CustomerId = customerId, AssetId = SeedData.ChampagneAssetId, Quantity = 6, AveragePurchasePrice = 4_000m });
        dbContext.SaveChanges();

        var preview = await new PurchasePreviewService(dbContext)
            .PreviewAsync(customerId, SeedData.BordeauxAssetId, 2, null);

        Assert.NotNull(preview);
        Assert.Equal("None", preview.Before.Level);

        // 4 x 12,500 = 50,000 of 75,200.
        Assert.Equal(66.49m, preview.After.LargestSharePercent);
        Assert.Equal("Medium", preview.After.Level);

        var warning = Assert.Single(preview.Warnings, item => item.Code == "ConcentrationIncrease");
        Assert.Equal("Medium", warning.Severity);
        Assert.Contains("from None to Medium", warning.Message);
    }

    [Fact]
    public async Task Preview_WarnsAboutInventoryPriceAndPendingRequests()
    {
        using var dbContext = TestDbContextFactory.Create();
        dbContext.PurchaseRequests.Add(new PurchaseRequest
        {
            Id = Guid.NewGuid(),
            CustomerId = SeedData.CustomerEmmaId,
            AssetId = SeedData.BordeauxAssetId,
            Quantity = 1,
            RequestedPrice = 12_000m,
            Status = PurchaseRequestStatus.Pending,
            RequestedBy = "Kris",
            CreatedUtc = DateTime.UtcNow
        });
        dbContext.SaveChanges();

        // 10 Burgundy, but only 9 in inventory; 7,000 is 21.35% below the 8,900 market price.
        var preview = await new PurchasePreviewService(dbContext)
            .PreviewAsync(SeedData.CustomerEmmaId, SeedData.BurgundyAssetId, 10, 7_000m);

        Assert.NotNull(preview);
        Assert.Equal("Requested", preview.PriceSource);
        Assert.Equal(70_000m, preview.EstimatedCost);
        Assert.False(preview.Inventory.IsSufficient);
        Assert.Equal(9, preview.Inventory.QuantityAvailable);
        Assert.Equal(1, preview.PendingRequests.Count);
        Assert.Equal(12_000m, preview.PendingRequests.Exposure);

        var codes = preview.Warnings.Select(warning => warning.Code).ToList();
        Assert.Contains("InsufficientInventory", codes);
        Assert.Contains("PriceDeviation", codes);
        Assert.Contains("PendingRequests", codes);
        Assert.Contains("-21.35%", preview.Warnings.Single(warning => warning.Code == "PriceDeviation").Message);
    }

    [Fact]
    public async Task Preview_DoesNotWarnAboutAPriceWithinTenPercent()
    {
        using var dbContext = TestDbContextFactory.Create();

        var preview = await new PurchasePreviewService(dbContext)
            .PreviewAsync(SeedData.CustomerEmmaId, SeedData.BordeauxAssetId, 1, 11_250m);

        Assert.NotNull(preview);
        Assert.DoesNotContain(preview.Warnings, warning => warning.Code == "PriceDeviation");
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(-1, null)]
    [InlineData(10_001, null)]
    [InlineData(1, 0)]
    [InlineData(1, -5)]
    public async Task Preview_RejectsInvalidInput(int quantity, int? requestedPrice)
    {
        using var dbContext = TestDbContextFactory.Create();

        await Assert.ThrowsAsync<ArgumentException>(() => new PurchasePreviewService(dbContext)
            .PreviewAsync(SeedData.CustomerEmmaId, SeedData.BordeauxAssetId, quantity, requestedPrice));
    }

    [Fact]
    public async Task Preview_ReturnsNullForAnUnknownCustomerOrAsset()
    {
        using var dbContext = TestDbContextFactory.Create();
        var service = new PurchasePreviewService(dbContext);

        Assert.Null(await service.PreviewAsync(Guid.NewGuid(), SeedData.BordeauxAssetId, 1, null));
        Assert.Null(await service.PreviewAsync(SeedData.CustomerEmmaId, Guid.NewGuid(), 1, null));
    }

    [Fact]
    public async Task Preview_ChangesNothing()
    {
        using var dbContext = TestDbContextFactory.Create();
        var holdingsBefore = dbContext.Holdings.Select(holding => new { holding.Id, holding.Quantity }).ToList();

        await new PurchasePreviewService(dbContext)
            .PreviewAsync(SeedData.CustomerEmmaId, SeedData.BordeauxAssetId, 5, 12_000m);

        Assert.False(dbContext.ChangeTracker.HasChanges());
        Assert.Empty(dbContext.PurchaseRequests);
        Assert.Equal(holdingsBefore, dbContext.Holdings.Select(holding => new { holding.Id, holding.Quantity }).ToList());
        Assert.Equal(18, dbContext.InventoryItems.Single(item => item.AssetId == SeedData.BordeauxAssetId).QuantityAvailable);
    }
}
