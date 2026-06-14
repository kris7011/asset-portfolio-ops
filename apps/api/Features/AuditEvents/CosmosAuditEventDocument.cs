using AssetPortfolioOps.Api.Domain;
using Newtonsoft.Json;

namespace AssetPortfolioOps.Api.Features.AuditEvents;

public sealed class CosmosAuditEventDocument
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("entityId")]
    public string EntityId { get; set; } = string.Empty;

    [JsonProperty("entityType")]
    public string EntityType { get; set; } = string.Empty;

    [JsonProperty("action")]
    public string Action { get; set; } = string.Empty;

    [JsonProperty("performedBy")]
    public string PerformedBy { get; set; } = string.Empty;

    [JsonProperty("timestampUtc")]
    public DateTime TimestampUtc { get; set; }

    [JsonProperty("metadata")]
    public Dictionary<string, string> Metadata { get; set; } = new();

    public static CosmosAuditEventDocument FromDomain(AuditEvent auditEvent)
    {
        return new CosmosAuditEventDocument
        {
            Id = auditEvent.Id,
            EntityId = auditEvent.EntityId,
            EntityType = auditEvent.EntityType,
            Action = auditEvent.Action,
            PerformedBy = auditEvent.PerformedBy,
            TimestampUtc = auditEvent.TimestampUtc,
            Metadata = auditEvent.Metadata
        };
    }

    public AuditEvent ToDomain()
    {
        return new AuditEvent
        {
            Id = Id,
            EntityId = EntityId,
            EntityType = EntityType,
            Action = Action,
            PerformedBy = PerformedBy,
            TimestampUtc = TimestampUtc,
            Metadata = Metadata
        };
    }
}
