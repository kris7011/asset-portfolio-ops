using AssetPortfolioOps.Api.Data;
using AssetPortfolioOps.Api.Domain;
using AssetPortfolioOps.Api.Features.Valuations;

namespace AssetPortfolioOps.Api.Tests;

public sealed class ValuationServiceTests
{
    [Fact]
    public void SeedData_IsADeterministicDemonstrationSeriesEndingAtTheMarketPrice()
    {
        foreach (var asset in SeedData.Assets)
        {
            var series = ValuationSeedData.Valuations
                .Where(valuation => valuation.AssetId == asset.Id)
                .OrderBy(valuation => valuation.ValuationDate)
                .ToList();

            Assert.Equal(24, series.Count);
            Assert.Equal(asset.MarketPrice, series[^1].Price);
            Assert.Equal(new DateOnly(2026, 9, 30), series[^1].ValuationDate);
            Assert.Equal(series.Count, series.Select(valuation => valuation.ValuationDate).Distinct().Count());
            Assert.All(series, valuation =>
            {
                Assert.True(valuation.IsDemonstrationData);
                Assert.Contains("not market prices", valuation.Source);
                Assert.True(valuation.Price > 0);
                Assert.Equal(0m, valuation.Price % 50m);
            });
        }
    }

    [Fact]
    public async Task GetAssetHistory_ReturnsOrderedPointsAndSummary()
    {
        using var dbContext = TestDbContextFactory.Create();
        var service = new ValuationService(dbContext);

        var history = await service.GetAssetHistoryAsync(SeedData.BordeauxAssetId, null, null);

        Assert.NotNull(history);
        Assert.Equal(24, history.Points.Count);
        Assert.True(history.Points.Zip(history.Points.Skip(1)).All(pair => pair.First.Date < pair.Second.Date));
        Assert.True(history.IsDemonstrationData);
        Assert.Equal(12_500m, history.LatestPrice);
        Assert.Equal(history.LatestPrice - history.FirstPrice, history.ChangeAmount);
        Assert.Equal(
            Math.Round(history.ChangeAmount!.Value / history.FirstPrice!.Value * 100m, 2),
            history.ChangePercent);
        Assert.Equal(history.Points.Min(point => point.Price), history.MinPrice);
        Assert.Equal(history.Points.Max(point => point.Price), history.MaxPrice);
    }

    [Fact]
    public async Task GetAssetHistory_FiltersByDateRange()
    {
        using var dbContext = TestDbContextFactory.Create();
        var service = new ValuationService(dbContext);

        var history = await service.GetAssetHistoryAsync(
            SeedData.ChampagneAssetId,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 6, 30));

        Assert.NotNull(history);
        Assert.Equal(6, history.Points.Count);
        Assert.Equal(new DateOnly(2026, 1, 31), history.Points[0].Date);
        Assert.Equal(new DateOnly(2026, 6, 30), history.Points[^1].Date);
    }

    [Fact]
    public async Task GetAssetHistory_RejectsAnInvertedRange_AndReturnsNullForAnUnknownAsset()
    {
        using var dbContext = TestDbContextFactory.Create();
        var service = new ValuationService(dbContext);

        await Assert.ThrowsAsync<ArgumentException>(() => service.GetAssetHistoryAsync(
            SeedData.ChampagneAssetId,
            new DateOnly(2026, 6, 30),
            new DateOnly(2026, 1, 1)));

        Assert.Null(await service.GetAssetHistoryAsync(Guid.NewGuid(), null, null));
    }

    [Fact]
    public async Task GetAssetHistory_ReturnsAnEmptySummaryWhenNoValuationsMatch()
    {
        using var dbContext = TestDbContextFactory.Create();
        var service = new ValuationService(dbContext);

        var history = await service.GetAssetHistoryAsync(
            SeedData.ChampagneAssetId,
            new DateOnly(2020, 1, 1),
            new DateOnly(2020, 12, 31));

        Assert.NotNull(history);
        Assert.Empty(history.Points);
        Assert.Null(history.ChangePercent);
        Assert.False(history.IsDemonstrationData);
    }

    [Fact]
    public async Task GetPortfolioHistory_ValuesCurrentHoldingsAtEachHistoricalPrice()
    {
        using var dbContext = TestDbContextFactory.Create();
        var service = new ValuationService(dbContext);

        var history = await service.GetPortfolioHistoryAsync(SeedData.CustomerEmmaId);

        Assert.NotNull(history);
        Assert.Equal(24, history.Points.Count);
        Assert.Equal(ValuationService.PortfolioBasis, history.Basis);
        Assert.True(history.IsDemonstrationData);

        // The last point equals today's portfolio value: 6 x 12,500 + 3 x 8,900.
        Assert.Equal(101_700m, history.Points[^1].TotalValue);

        var first = history.Points[0];
        var bordeaux = ValuationSeedData.Valuations.Single(valuation =>
            valuation.AssetId == SeedData.BordeauxAssetId && valuation.ValuationDate == first.Date);
        var burgundy = ValuationSeedData.Valuations.Single(valuation =>
            valuation.AssetId == SeedData.BurgundyAssetId && valuation.ValuationDate == first.Date);

        Assert.Equal(6 * bordeaux.Price + 3 * burgundy.Price, first.TotalValue);
    }

    [Fact]
    public async Task GetPortfolioHistory_CarriesTheLastPriceForwardAndSkipsDatesBeforeEveryAssetHasOne()
    {
        using var dbContext = TestDbContextFactory.Create();

        // Burgundy has no valuation in the first month and none in the last one.
        dbContext.AssetValuations.RemoveRange(dbContext.AssetValuations.Where(valuation =>
            valuation.AssetId == SeedData.BurgundyAssetId &&
            (valuation.ValuationDate == new DateOnly(2024, 10, 31) || valuation.ValuationDate == new DateOnly(2026, 9, 30))));
        dbContext.SaveChanges();

        var history = await new ValuationService(dbContext).GetPortfolioHistoryAsync(SeedData.CustomerEmmaId);

        Assert.NotNull(history);
        Assert.Equal(23, history.Points.Count);
        Assert.Equal(new DateOnly(2024, 11, 30), history.Points[0].Date);

        var burgundyAugust = ValuationSeedData.Valuations.Single(valuation =>
            valuation.AssetId == SeedData.BurgundyAssetId && valuation.ValuationDate == new DateOnly(2026, 8, 31));

        Assert.Equal(6 * 12_500m + 3 * burgundyAugust.Price, history.Points[^1].TotalValue);
    }

    [Fact]
    public async Task GetPortfolioHistory_ReturnsNullForAnUnknownCustomer()
    {
        using var dbContext = TestDbContextFactory.Create();

        Assert.Null(await new ValuationService(dbContext).GetPortfolioHistoryAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetPortfolioHistory_IsEmptyForACustomerWithoutHoldings()
    {
        using var dbContext = TestDbContextFactory.Create();
        var customerId = Guid.NewGuid();
        dbContext.Customers.Add(new Customer { Id = customerId, Name = "New", Email = "new@example.com" });
        dbContext.SaveChanges();

        var history = await new ValuationService(dbContext).GetPortfolioHistoryAsync(customerId);

        Assert.NotNull(history);
        Assert.Empty(history.Points);
    }
}
