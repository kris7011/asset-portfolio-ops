using AssetPortfolioOps.Api.Data;
using AssetPortfolioOps.Api.Domain;
using AssetPortfolioOps.Api.Features.RiskIndicators;

namespace AssetPortfolioOps.Api.Tests;

public sealed class RiskIndicatorServiceTests
{
    [Fact]
    public async Task GetForCustomerAsync_ReturnsHighConcentrationRisk()
    {
        using var dbContext = TestDbContextFactory.Create();
        var service = new RiskIndicatorService(dbContext);

        var indicators = await service.GetForCustomerAsync(SeedData.CustomerEmmaId);

        Assert.NotNull(indicators);
        Assert.Contains(indicators, indicator =>
            indicator.Type == "HighConcentration" &&
            indicator.Severity == "High" &&
            indicator.Message.Contains("Bordeaux Premier Cru 2016"));
    }

    [Fact]
    public async Task GetForCustomerAsync_ReturnsLowInventoryWarning()
    {
        using var dbContext = TestDbContextFactory.Create();
        var service = new RiskIndicatorService(dbContext);

        var indicators = await service.GetForCustomerAsync(SeedData.CustomerEmmaId);

        Assert.NotNull(indicators);
        Assert.Contains(indicators, indicator =>
            indicator.Type == "LowInventory" &&
            indicator.Message.Contains("Burgundy Grand Cru 2019"));
    }

    [Fact]
    public async Task GetForCustomerAsync_ReturnsNull_WhenCustomerDoesNotExist()
    {
        using var dbContext = TestDbContextFactory.Create();
        var service = new RiskIndicatorService(dbContext);

        var indicators = await service.GetForCustomerAsync(Guid.NewGuid());

        Assert.Null(indicators);
    }

    [Fact]
    public async Task GetForCustomerAsync_ReturnsNoRiskDetected_WhenCustomerHasNoHoldingsOrRequests()
    {
        using var dbContext = TestDbContextFactory.Create();
        var customerId = Guid.NewGuid();
        dbContext.Customers.Add(new Customer
        {
            Id = customerId,
            Name = "New Customer",
            Email = "new.customer@example.com"
        });
        dbContext.SaveChanges();
        var service = new RiskIndicatorService(dbContext);

        var indicators = await service.GetForCustomerAsync(customerId);

        Assert.NotNull(indicators);
        var indicator = Assert.Single(indicators);
        Assert.Equal("NoRiskDetected", indicator.Type);
        Assert.Equal("Low", indicator.Severity);
    }

    [Theory]
    [InlineData(1, 24999, "Low")]
    [InlineData(1, 25000, "Medium")]
    [InlineData(2, 25000, "High")]
    public async Task GetForCustomerAsync_ScalesPendingExposureSeverityWithRequestValue(
        int quantity,
        int requestedPrice,
        string expectedSeverity)
    {
        using var dbContext = TestDbContextFactory.Create();
        dbContext.PurchaseRequests.Add(new PurchaseRequest
        {
            Id = Guid.NewGuid(),
            CustomerId = SeedData.CustomerEmmaId,
            AssetId = SeedData.BordeauxAssetId,
            Quantity = quantity,
            RequestedPrice = requestedPrice,
            RequestedBy = "Kris",
            Status = PurchaseRequestStatus.Pending,
            CreatedUtc = DateTime.UtcNow
        });
        dbContext.SaveChanges();
        var service = new RiskIndicatorService(dbContext);

        var indicators = await service.GetForCustomerAsync(SeedData.CustomerEmmaId);

        Assert.NotNull(indicators);
        var exposure = Assert.Single(indicators, indicator => indicator.Type == "PendingRequestExposure");
        Assert.Equal(expectedSeverity, exposure.Severity);
    }

    [Fact]
    public async Task GetForCustomerAsync_IgnoresRequestsThatAreNoLongerPending()
    {
        using var dbContext = TestDbContextFactory.Create();
        dbContext.PurchaseRequests.Add(new PurchaseRequest
        {
            Id = Guid.NewGuid(),
            CustomerId = SeedData.CustomerEmmaId,
            AssetId = SeedData.BordeauxAssetId,
            Quantity = 10,
            RequestedPrice = 12500m,
            RequestedBy = "Kris",
            Status = PurchaseRequestStatus.Approved,
            CreatedUtc = DateTime.UtcNow
        });
        dbContext.SaveChanges();
        var service = new RiskIndicatorService(dbContext);

        var indicators = await service.GetForCustomerAsync(SeedData.CustomerEmmaId);

        Assert.NotNull(indicators);
        Assert.DoesNotContain(indicators, indicator => indicator.Type == "PendingRequestExposure");
    }
}
