namespace AssetPortfolioOps.Api.Domain;

/// <summary>
/// The market price of one unit of an asset on a given date.
/// </summary>
public sealed class AssetValuation
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public DateOnly ValuationDate { get; set; }
    public decimal Price { get; set; }

    /// <summary>
    /// Where the price comes from, shown with every valuation.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// True for the synthetic seed series. Never presented as real market data.
    /// </summary>
    public bool IsDemonstrationData { get; set; }
}
