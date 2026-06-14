using AssetPortfolioOps.Api.Domain;

namespace AssetPortfolioOps.Api.Features.AuditEvents;

public interface IAuditEventStore
{
    Task AddAsync(AuditEvent auditEvent);
    Task<IReadOnlyCollection<AuditEvent>> GetAllAsync();
}
