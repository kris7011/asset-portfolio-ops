using AssetPortfolioOps.Api.Data;
using AssetPortfolioOps.Api.Domain;
using AssetPortfolioOps.Api.Features.AuditEvents;
using Microsoft.EntityFrameworkCore;

namespace AssetPortfolioOps.Api.Features.PurchaseRequests;

public sealed class PurchaseRequestService(
    AppDbContext dbContext,
    IAuditEventStore auditEventStore) : IPurchaseRequestService
{
    public IReadOnlyCollection<PurchaseRequest> GetAll()
    {
        return dbContext.PurchaseRequests
            .AsNoTracking()
            .OrderByDescending(request => request.CreatedUtc)
            .ToList();
    }

    public async Task<PurchaseRequest> CreateAsync(CreatePurchaseRequestRequest request)
    {
        Validate(request);

        var purchaseRequest = new PurchaseRequest
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            AssetId = request.AssetId,
            Quantity = request.Quantity,
            RequestedPrice = request.RequestedPrice,
            Status = PurchaseRequestStatus.Pending,
            RequestedBy = request.RequestedBy.Trim(),
            CreatedUtc = DateTime.UtcNow
        };

        dbContext.PurchaseRequests.Add(purchaseRequest);
        await dbContext.SaveChangesAsync();

        await auditEventStore.AddAsync(new AuditEvent
        {
            EntityId = purchaseRequest.Id.ToString(),
            EntityType = nameof(PurchaseRequest),
            Action = "Created",
            PerformedBy = purchaseRequest.RequestedBy,
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

    private void Validate(CreatePurchaseRequestRequest request)
    {
        if (request.CustomerId == Guid.Empty)
        {
            throw new InvalidOperationException("CustomerId is required.");
        }

        if (request.AssetId == Guid.Empty)
        {
            throw new InvalidOperationException("AssetId is required.");
        }

        if (request.Quantity <= 0)
        {
            throw new InvalidOperationException("Quantity must be greater than zero.");
        }

        if (request.RequestedPrice <= 0)
        {
            throw new InvalidOperationException("RequestedPrice must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(request.RequestedBy))
        {
            throw new InvalidOperationException("RequestedBy is required.");
        }

        if (!dbContext.Customers.Any(customer => customer.Id == request.CustomerId))
        {
            throw new InvalidOperationException("Customer was not found.");
        }

        if (!dbContext.Assets.Any(asset => asset.Id == request.AssetId))
        {
            throw new InvalidOperationException("Asset was not found.");
        }
    }
}
