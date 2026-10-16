using AssetPortfolioOps.Api.Domain;
using AssetPortfolioOps.Api.Features.AuditEvents;

namespace AssetPortfolioOps.Api.Tests;

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
