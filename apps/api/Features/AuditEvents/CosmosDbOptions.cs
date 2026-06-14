namespace AssetPortfolioOps.Api.Features.AuditEvents;

public sealed class CosmosDbOptions
{
    public bool Enabled { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = "asset-portfolio-ops";
    public string AuditEventsContainerName { get; set; } = "audit-events";
}
