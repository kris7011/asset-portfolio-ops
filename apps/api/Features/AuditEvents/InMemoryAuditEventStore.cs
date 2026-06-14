using AssetPortfolioOps.Api.Domain;

namespace AssetPortfolioOps.Api.Features.AuditEvents;

public sealed class InMemoryAuditEventStore : IAuditEventStore
{
    private readonly List<AuditEvent> _events = new();
    private readonly object _syncRoot = new();

    public Task AddAsync(AuditEvent auditEvent)
    {
        lock (_syncRoot)
        {
            _events.Add(auditEvent);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<AuditEvent>> GetAllAsync()
    {
        lock (_syncRoot)
        {
            return Task.FromResult<IReadOnlyCollection<AuditEvent>>(
                _events
                    .OrderByDescending(auditEvent => auditEvent.TimestampUtc)
                    .ToList());
        }
    }
}
