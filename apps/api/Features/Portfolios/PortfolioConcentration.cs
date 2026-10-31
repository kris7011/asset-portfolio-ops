namespace AssetPortfolioOps.Api.Features.Portfolios;

public sealed record PositionValue(Guid AssetId, string AssetName, int Quantity, decimal MarketValue);

public sealed record PositionShare(
    Guid AssetId,
    string AssetName,
    int Quantity,
    decimal MarketValue,
    decimal SharePercent);

/// <summary>
/// How much of a portfolio's market value sits in its largest position.
/// </summary>
/// <param name="Level">"None" below 60 %, "Medium" from 60 %, "High" from 70 %.</param>
public sealed record ConcentrationResult(
    decimal TotalMarketValue,
    IReadOnlyList<PositionShare> Positions,
    PositionShare? LargestPosition,
    decimal LargestSharePercent,
    string Level);

/// <summary>
/// The single definition of portfolio concentration, used by the risk
/// indicators and by the purchase preview so both always agree.
/// </summary>
public static class PortfolioConcentration
{
    public const decimal MediumThresholdPercent = 60m;
    public const decimal HighThresholdPercent = 70m;

    public static ConcentrationResult Calculate(IEnumerable<PositionValue> positions)
    {
        var grouped = positions
            .GroupBy(position => position.AssetId)
            .Select(group => new PositionValue(
                group.Key,
                group.First().AssetName,
                group.Sum(position => position.Quantity),
                group.Sum(position => position.MarketValue)))
            .ToList();

        var total = grouped.Sum(position => position.MarketValue);

        var shares = grouped
            .Select(position => new PositionShare(
                position.AssetId,
                position.AssetName,
                position.Quantity,
                position.MarketValue,
                total > 0 ? position.MarketValue / total * 100m : 0m))
            .OrderByDescending(position => position.MarketValue)
            .ThenBy(position => position.AssetName, StringComparer.Ordinal)
            .ToList();

        var largest = shares.FirstOrDefault();
        var largestShare = largest?.SharePercent ?? 0m;

        return new ConcentrationResult(total, shares, largest, largestShare, LevelFor(largestShare));
    }

    public static string LevelFor(decimal largestSharePercent)
    {
        return largestSharePercent >= HighThresholdPercent
            ? "High"
            : largestSharePercent >= MediumThresholdPercent
                ? "Medium"
                : "None";
    }

    public static int Rank(string level)
    {
        return level switch
        {
            "High" => 2,
            "Medium" => 1,
            _ => 0
        };
    }
}
