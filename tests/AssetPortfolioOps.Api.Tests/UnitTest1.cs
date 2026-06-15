using AssetPortfolioOps.Api.Data;
using AssetPortfolioOps.Api.Domain;
using AssetPortfolioOps.Api.Features.AuditEvents;
using AssetPortfolioOps.Api.Features.Portfolios;
using AssetPortfolioOps.Api.Features.PurchaseRequests;
using AssetPortfolioOps.Api.Features.RiskIndicators;
using Microsoft.EntityFrameworkCore;

namespace AssetPortfolioOps.Api.Tests;

public sealed class PortfolioServiceTests
{
    [Fact]
    public void GetPortfolio_ReturnsExpectedMarketValue()
    {
        using var dbContext = TestDbContextFactory.Create();
        var service = new PortfolioService(dbContext);

        var portfolio = service.GetPortfolio(SeedData.CustomerEmmaId);

        Assert.NotNull(portfolio);
        Assert.Equal(portfolio.Items.Sum(item => item.TotalMarketValue), portfolio.TotalMarketValue);
        Assert.Equal(portfolio.Items.Sum(item => item.GainLoss), portfolio.TotalGainLoss);
        Assert.True(portfolio.TotalMarketValue > 0);
    }

    [Fact]
    public void GetPortfolio_ReturnsNull_WhenCustomerDoesNotExist()
    {
        using var dbContext = TestDbContextFactory.Create();
        var service = new PortfolioService(dbContext);

        var portfolio = service.GetPortfolio(Guid.NewGuid());

        Assert.Null(portfolio);
    }
}

public sealed class PurchaseRequestServiceTests
{
    [Fact]
    public async Task CreateAsync_CreatesPendingRequest()
    {
        using var dbContext = TestDbContextFactory.Create();
        var auditEventStore = new InMemoryAuditEventStore();
        var service = new PurchaseRequestService(dbContext, auditEventStore);

        var request = new CreatePurchaseRequestRequest
        {
            CustomerId = SeedData.CustomerEmmaId,
            AssetId = SeedData.BordeauxAssetId,
            Quantity = 2,
            RequestedPrice = 12300m,
            RequestedBy = "Kris"
        };

        var createdRequest = await service.CreateAsync(request);

        Assert.Equal(PurchaseRequestStatus.Pending, createdRequest.Status);
        Assert.Single(dbContext.PurchaseRequests);
        Assert.Equal(request.CustomerId, createdRequest.CustomerId);
        Assert.Equal(request.AssetId, createdRequest.AssetId);
    }

    [Fact]
    public async Task CreateAsync_WritesAuditEvent()
    {
        using var dbContext = TestDbContextFactory.Create();
        var auditEventStore = new InMemoryAuditEventStore();
        var service = new PurchaseRequestService(dbContext, auditEventStore);

        var request = new CreatePurchaseRequestRequest
        {
            CustomerId = SeedData.CustomerEmmaId,
            AssetId = SeedData.BordeauxAssetId,
            Quantity = 2,
            RequestedPrice = 12300m,
            RequestedBy = "Kris"
        };

        var createdRequest = await service.CreateAsync(request);
        var auditEvents = await auditEventStore.GetAllAsync();

        Assert.Contains(auditEvents, auditEvent =>
            auditEvent.EntityId == createdRequest.Id.ToString() &&
            auditEvent.EntityType == nameof(PurchaseRequest) &&
            auditEvent.Action == "Created");
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenQuantityIsInvalid()
    {
        using var dbContext = TestDbContextFactory.Create();
        var auditEventStore = new InMemoryAuditEventStore();
        var service = new PurchaseRequestService(dbContext, auditEventStore);

        var request = new CreatePurchaseRequestRequest
        {
            CustomerId = SeedData.CustomerEmmaId,
            AssetId = SeedData.BordeauxAssetId,
            Quantity = 0,
            RequestedPrice = 12300m,
            RequestedBy = "Kris"
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request));
    }

    [Fact]
    public async Task ApproveAsync_ChangesStatusToApproved()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var auditEventStore = new InMemoryAuditEventStore();
        var service = new PurchaseRequestService(dbContext, auditEventStore);

        var createdRequest = await service.CreateAsync(new CreatePurchaseRequestRequest
        {
            CustomerId = SeedData.CustomerEmmaId,
            AssetId = SeedData.BordeauxAssetId,
            Quantity = 2,
            RequestedPrice = 12300m,
            RequestedBy = "Kris"
        });

        var approvedRequest = await service.ApproveAsync(
            createdRequest.Id,
            "Katrine");

        Assert.Equal(PurchaseRequestStatus.Approved, approvedRequest.Status);
    }

    [Fact]
    public async Task RejectAsync_ChangesStatusToRejected()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var auditEventStore = new InMemoryAuditEventStore();
        var service = new PurchaseRequestService(dbContext, auditEventStore);

        var createdRequest = await service.CreateAsync(new CreatePurchaseRequestRequest
        {
            CustomerId = SeedData.CustomerEmmaId,
            AssetId = SeedData.BordeauxAssetId,
            Quantity = 2,
            RequestedPrice = 12300m,
            RequestedBy = "Kris"
        });

        var rejectedRequest = await service.RejectAsync(
            createdRequest.Id,
            "Katrine");

        Assert.Equal(PurchaseRequestStatus.Rejected, rejectedRequest.Status);
    }

    [Fact]
    public async Task CompleteAsync_ChangesApprovedRequestToCompleted()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var auditEventStore = new InMemoryAuditEventStore();
        var service = new PurchaseRequestService(dbContext, auditEventStore);

        var createdRequest = await service.CreateAsync(new CreatePurchaseRequestRequest
        {
            CustomerId = SeedData.CustomerEmmaId,
            AssetId = SeedData.BordeauxAssetId,
            Quantity = 2,
            RequestedPrice = 12300m,
            RequestedBy = "Kris"
        });

        var approvedRequest = await service.ApproveAsync(
            createdRequest.Id,
            "Katrine");

        var completedRequest = await service.CompleteAsync(
            approvedRequest.Id,
            "Katrine");

        Assert.Equal(PurchaseRequestStatus.Completed, completedRequest.Status);
    }

    [Fact]
    public async Task CompleteAsync_Throws_WhenRequestIsPending()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var auditEventStore = new InMemoryAuditEventStore();
        var service = new PurchaseRequestService(dbContext, auditEventStore);

        var createdRequest = await service.CreateAsync(new CreatePurchaseRequestRequest
        {
            CustomerId = SeedData.CustomerEmmaId,
            AssetId = SeedData.BordeauxAssetId,
            Quantity = 2,
            RequestedPrice = 12300m,
            RequestedBy = "Kris"
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CompleteAsync(createdRequest.Id, "Katrine"));
    }

    [Fact]
    public async Task ApproveAsync_WritesAuditEvent()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var auditEventStore = new InMemoryAuditEventStore();
        var service = new PurchaseRequestService(dbContext, auditEventStore);

        var createdRequest = await service.CreateAsync(new CreatePurchaseRequestRequest
        {
            CustomerId = SeedData.CustomerEmmaId,
            AssetId = SeedData.BordeauxAssetId,
            Quantity = 2,
            RequestedPrice = 12300m,
            RequestedBy = "Kris"
        });

        await service.ApproveAsync(createdRequest.Id, "Katrine");

        var auditEvents = await auditEventStore.GetAllAsync();

        Assert.Contains(auditEvents, auditEvent =>
            auditEvent.EntityId == createdRequest.Id.ToString() &&
            auditEvent.Action == PurchaseRequestStatus.Approved.ToString());
    }
}

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
}

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
}

public sealed class AuditEventStoreTests
{
    [Fact]
    public async Task InMemoryAuditEventStore_ReturnsNewestEventsFirst()
    {
        var auditEventStore = new InMemoryAuditEventStore();

        await auditEventStore.AddAsync(new AuditEvent
        {
            EntityId = "entity-1",
            EntityType = "PurchaseRequest",
            Action = "Created",
            PerformedBy = "Kris",
            TimestampUtc = new DateTime(2026, 6, 14, 8, 0, 0, DateTimeKind.Utc)
        });

        await auditEventStore.AddAsync(new AuditEvent
        {
            EntityId = "entity-2",
            EntityType = "PurchaseRequest",
            Action = "Approved",
            PerformedBy = "Kris",
            TimestampUtc = new DateTime(2026, 6, 14, 9, 0, 0, DateTimeKind.Utc)
        });

        var auditEvents = await auditEventStore.GetAllAsync();

        Assert.Collection(
            auditEvents,
            first => Assert.Equal("Approved", first.Action),
            second => Assert.Equal("Created", second.Action));
    }
}
