namespace AssetPortfolioOps.Api.Features.RiskIndicators;

public sealed record RiskIndicatorResponse(
    string Type,
    string Severity,
    string Title,
    string Message);