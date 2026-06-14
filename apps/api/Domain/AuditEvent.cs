namespace AssetPortfolioOps.Api.Domain;

public sealed class AuditEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string EntityId { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string PerformedBy { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}
