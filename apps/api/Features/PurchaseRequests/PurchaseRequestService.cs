using AssetPortfolioOps.Api.Data;
using AssetPortfolioOps.Api.Domain;
using AssetPortfolioOps.Api.Features.AuditEvents;
using Microsoft.EntityFrameworkCore;

namespace AssetPortfolioOps.Api.Features.PurchaseRequests;

public sealed class PurchaseRequestService(
    AppDbContext dbContext,
    IAuditEventStore auditEventStore) : IPurchaseRequestService
{
    public async Task<IReadOnlyList<PurchaseRequest>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.PurchaseRequests
            .AsNoTracking()
            .OrderByDescending(request => request.CreatedUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<PurchaseRequest> CreateAsync(
        CreatePurchaseRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero.");
        }

        if (request.RequestedPrice <= 0)
        {
            throw new ArgumentException("Requested price must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(request.RequestedBy))
        {
            throw new ArgumentException("Requested by is required.");
        }

        var customerExists = await dbContext.Customers
            .AnyAsync(customer => customer.Id == request.CustomerId, cancellationToken);

        if (!customerExists)
        {
            throw new ArgumentException("Customer does not exist.");
        }

        var assetExists = await dbContext.Assets
            .AnyAsync(asset => asset.Id == request.AssetId, cancellationToken);

        if (!assetExists)
        {
            throw new ArgumentException("Asset does not exist.");
        }

        var purchaseRequest = new PurchaseRequest
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            AssetId = request.AssetId,
            Quantity = request.Quantity,
            RequestedPrice = request.RequestedPrice,
            RequestedBy = request.RequestedBy,
            Status = PurchaseRequestStatus.Pending,
            CreatedUtc = DateTime.UtcNow
        };

        dbContext.PurchaseRequests.Add(purchaseRequest);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditEventStore.AddAsync(new AuditEvent
        {
            EntityId = purchaseRequest.Id.ToString(),
            EntityType = nameof(PurchaseRequest),
            Action = "Created",
            PerformedBy = request.RequestedBy,
            TimestampUtc = DateTime.UtcNow,
            Metadata = new Dictionary<string, string>
            {
                ["customerId"] = purchaseRequest.CustomerId.ToString(),
                ["assetId"] = purchaseRequest.AssetId.ToString(),
                ["quantity"] = purchaseRequest.Quantity.ToString(),
                ["requestedPrice"] = purchaseRequest.RequestedPrice.ToString()
            }
        });

        return purchaseRequest;
    }

    public Task<PurchaseRequest> ApproveAsync(
        Guid id,
        string performedBy,
        CancellationToken cancellationToken = default)
    {
        return ChangeStatusAsync(
            id,
            PurchaseRequestStatus.Approved,
            performedBy,
            cancellationToken);
    }

    public Task<PurchaseRequest> RejectAsync(
        Guid id,
        string performedBy,
        CancellationToken cancellationToken = default)
    {
        return ChangeStatusAsync(
            id,
            PurchaseRequestStatus.Rejected,
            performedBy,
            cancellationToken);
    }

    public Task<PurchaseRequest> CompleteAsync(
        Guid id,
        string performedBy,
        CancellationToken cancellationToken = default)
    {
        return ChangeStatusAsync(
            id,
            PurchaseRequestStatus.Completed,
            performedBy,
            cancellationToken);
    }

    private async Task<PurchaseRequest> ChangeStatusAsync(
        Guid id,
        PurchaseRequestStatus newStatus,
        string performedBy,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(performedBy))
        {
            throw new ArgumentException("Performed by is required.");
        }

        var purchaseRequest = await dbContext.PurchaseRequests
            .FirstOrDefaultAsync(request => request.Id == id, cancellationToken);

        if (purchaseRequest is null)
        {
            throw new KeyNotFoundException("Purchase request was not found.");
        }

        ValidateStatusTransition(purchaseRequest.Status, newStatus);

        var previousStatus = purchaseRequest.Status;
        purchaseRequest.Status = newStatus;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditEventStore.AddAsync(new AuditEvent
        {
            EntityId = purchaseRequest.Id.ToString(),
            EntityType = nameof(PurchaseRequest),
            Action = newStatus.ToString(),
            PerformedBy = performedBy,
            TimestampUtc = DateTime.UtcNow,
            Metadata = new Dictionary<string, string>
            {
                ["previousStatus"] = previousStatus.ToString(),
                ["newStatus"] = newStatus.ToString()
            }
        });

        return purchaseRequest;
    }

    private static void ValidateStatusTransition(
        PurchaseRequestStatus currentStatus,
        PurchaseRequestStatus newStatus)
    {
        var isValidTransition =
            currentStatus == PurchaseRequestStatus.Pending &&
            (newStatus == PurchaseRequestStatus.Approved ||
             newStatus == PurchaseRequestStatus.Rejected);

        isValidTransition =
            isValidTransition ||
            currentStatus == PurchaseRequestStatus.Approved &&
            newStatus == PurchaseRequestStatus.Completed;

        if (!isValidTransition)
        {
            throw new InvalidOperationException(
                $"Cannot change purchase request status from {currentStatus} to {newStatus}.");
        }
    }
}