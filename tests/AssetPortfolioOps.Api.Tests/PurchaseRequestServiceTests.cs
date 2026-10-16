using AssetPortfolioOps.Api.Data;
using AssetPortfolioOps.Api.Domain;
using AssetPortfolioOps.Api.Features.AuditEvents;
using AssetPortfolioOps.Api.Features.PurchaseRequests;

namespace AssetPortfolioOps.Api.Tests;

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

    [Theory]
    [InlineData("zero-price")]
    [InlineData("missing-requested-by")]
    [InlineData("unknown-customer")]
    [InlineData("unknown-asset")]
    public async Task CreateAsync_Throws_AndStoresNothing_WhenInputIsInvalid(string invalidInput)
    {
        await using var dbContext = TestDbContextFactory.Create();
        var auditEventStore = new InMemoryAuditEventStore();
        var service = new PurchaseRequestService(dbContext, auditEventStore);

        var request = new CreatePurchaseRequestRequest
        {
            CustomerId = invalidInput == "unknown-customer" ? Guid.NewGuid() : SeedData.CustomerEmmaId,
            AssetId = invalidInput == "unknown-asset" ? Guid.NewGuid() : SeedData.BordeauxAssetId,
            Quantity = 2,
            RequestedPrice = invalidInput == "zero-price" ? 0m : 12300m,
            RequestedBy = invalidInput == "missing-requested-by" ? " " : "Kris"
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request));
        Assert.Empty(dbContext.PurchaseRequests);
        Assert.Empty(await auditEventStore.GetAllAsync());
    }

    [Theory]
    [InlineData(PurchaseRequestStatus.Approved, "approve")]
    [InlineData(PurchaseRequestStatus.Approved, "reject")]
    [InlineData(PurchaseRequestStatus.Rejected, "approve")]
    [InlineData(PurchaseRequestStatus.Rejected, "complete")]
    [InlineData(PurchaseRequestStatus.Completed, "reject")]
    [InlineData(PurchaseRequestStatus.Completed, "complete")]
    public async Task StatusChange_Throws_AndKeepsStatus_WhenTransitionIsNotAllowed(
        PurchaseRequestStatus currentStatus,
        string action)
    {
        await using var dbContext = TestDbContextFactory.Create();
        var auditEventStore = new InMemoryAuditEventStore();
        var service = new PurchaseRequestService(dbContext, auditEventStore);
        var purchaseRequest = await CreateRequestWithStatusAsync(service, currentStatus);
        var auditEventCountBefore = (await auditEventStore.GetAllAsync()).Count;

        Task<PurchaseRequest> changeStatus = action switch
        {
            "approve" => service.ApproveAsync(purchaseRequest.Id, "Katrine"),
            "reject" => service.RejectAsync(purchaseRequest.Id, "Katrine"),
            _ => service.CompleteAsync(purchaseRequest.Id, "Katrine")
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => changeStatus);
        Assert.Equal(currentStatus, dbContext.PurchaseRequests.Single().Status);
        Assert.Equal(auditEventCountBefore, (await auditEventStore.GetAllAsync()).Count);
    }

    [Fact]
    public async Task ApproveAsync_ThrowsKeyNotFound_WhenRequestDoesNotExist()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var service = new PurchaseRequestService(dbContext, new InMemoryAuditEventStore());

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.ApproveAsync(Guid.NewGuid(), "Katrine"));
    }

    [Fact]
    public async Task ApproveAsync_Throws_WhenPerformedByIsMissing()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var service = new PurchaseRequestService(dbContext, new InMemoryAuditEventStore());
        var purchaseRequest = await CreateRequestWithStatusAsync(service, PurchaseRequestStatus.Pending);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ApproveAsync(purchaseRequest.Id, ""));
    }

    private static async Task<PurchaseRequest> CreateRequestWithStatusAsync(
        PurchaseRequestService service,
        PurchaseRequestStatus status)
    {
        var purchaseRequest = await service.CreateAsync(new CreatePurchaseRequestRequest
        {
            CustomerId = SeedData.CustomerEmmaId,
            AssetId = SeedData.BordeauxAssetId,
            Quantity = 2,
            RequestedPrice = 12300m,
            RequestedBy = "Kris"
        });

        return status switch
        {
            PurchaseRequestStatus.Pending => purchaseRequest,
            PurchaseRequestStatus.Approved => await service.ApproveAsync(purchaseRequest.Id, "Katrine"),
            PurchaseRequestStatus.Rejected => await service.RejectAsync(purchaseRequest.Id, "Katrine"),
            _ => await service.CompleteAsync(
                (await service.ApproveAsync(purchaseRequest.Id, "Katrine")).Id,
                "Katrine")
        };
    }
}
