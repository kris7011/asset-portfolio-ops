using AssetPortfolioOps.Api.Data;
using AssetPortfolioOps.Api.Domain;
using AssetPortfolioOps.Api.Features.AuditEvents;
using AssetPortfolioOps.Api.Features.Portfolios;
using AssetPortfolioOps.Api.Features.PurchaseRequests;

namespace AssetPortfolioOps.Api.Tests;

public sealed class PortfolioServiceTests
{
    [Fact]
    public void GetPortfolio_ReturnsExpectedMarketValue()
    {
        var store = new InMemoryDataStore();
        var service = new PortfolioService(store);

        var portfolio = service.GetPortfolio(SeedData.CustomerEmmaId);

        Assert.NotNull(portfolio);
        Assert.Equal(portfolio.Items.Sum(item => item.TotalMarketValue), portfolio.TotalMarketValue);
        Assert.Equal(portfolio.Items.Sum(item => item.GainLoss), portfolio.TotalGainLoss);
        Assert.True(portfolio.TotalMarketValue > 0);
    }

    [Fact]
    public void GetPortfolio_ReturnsNull_WhenCustomerDoesNotExist()
    {
        var store = new InMemoryDataStore();
        var service = new PortfolioService(store);

        var portfolio = service.GetPortfolio(Guid.NewGuid());

        Assert.Null(portfolio);
    }
}

public sealed class PurchaseRequestServiceTests
{
    [Fact]
    public async Task CreateAsync_CreatesPendingRequest()
    {
        var store = new InMemoryDataStore();
        var auditEventStore = new InMemoryAuditEventStore();
        var service = new PurchaseRequestService(store, auditEventStore);

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
        Assert.Equal(1, store.PurchaseRequests.Count);
        Assert.Equal(request.CustomerId, createdRequest.CustomerId);
        Assert.Equal(request.AssetId, createdRequest.AssetId);
    }

    [Fact]
    public async Task CreateAsync_WritesAuditEvent()
    {
        var store = new InMemoryDataStore();
        var auditEventStore = new InMemoryAuditEventStore();
        var service = new PurchaseRequestService(store, auditEventStore);

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
        var store = new InMemoryDataStore();
        var auditEventStore = new InMemoryAuditEventStore();
        var service = new PurchaseRequestService(store, auditEventStore);

        var request = new CreatePurchaseRequestRequest
        {
            CustomerId = SeedData.CustomerEmmaId,
            AssetId = SeedData.BordeauxAssetId,
            Quantity = 0,
            RequestedPrice = 12300m,
            RequestedBy = "Kris"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(request));
    }
}
