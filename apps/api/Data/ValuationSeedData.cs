using AssetPortfolioOps.Api.Domain;

namespace AssetPortfolioOps.Api.Data;

/// <summary>
/// A synthetic, deterministic price history for the demo assets.
/// </summary>
/// <remarks>
/// These are not market prices. Each series is generated from a fixed
/// formula (a linear trend plus a seasonal wave, rounded to 50 DKK) so that
/// the demo, the tests and the screenshots always show the same numbers.
/// The last point of every series equals the asset's current market price,
/// so the history and the portfolio value agree.
/// </remarks>
public static class ValuationSeedData
{
    public const string DemonstrationSource = "Demonstration data (synthetic series, not market prices)";

    /// <summary>Month-end valuation dates, oldest first.</summary>
    public static IReadOnlyList<DateOnly> ValuationDates { get; } = Enumerable
        .Range(0, 24)
        .Select(month => LastDayOfMonth(new DateOnly(2024, 10, 1).AddMonths(month)))
        .ToList();

    // Total change over the 24 months, and the size of the seasonal wave,
    // relative to the current market price.
    private static readonly IReadOnlyList<(Guid AssetId, decimal TotalTrend, decimal Wave)> Series =
    [
        (SeedData.BordeauxAssetId, 0.22m, 0.03m),
        (SeedData.BurgundyAssetId, 0.12m, 0.05m),
        (SeedData.ChampagneAssetId, -0.06m, 0.02m)
    ];

    public static IReadOnlyList<AssetValuation> Valuations { get; } = Build();

    private static List<AssetValuation> Build()
    {
        var valuations = new List<AssetValuation>();
        var lastIndex = ValuationDates.Count - 1;

        for (var seriesIndex = 0; seriesIndex < Series.Count; seriesIndex++)
        {
            var (assetId, totalTrend, wave) = Series[seriesIndex];
            var currentPrice = SeedData.Assets.Single(asset => asset.Id == assetId).MarketPrice;

            for (var index = 0; index <= lastIndex; index++)
            {
                var monthsBeforeNow = lastIndex - index;
                var trendFactor = 1m - totalTrend * monthsBeforeNow / lastIndex;
                var waveFactor = wave * (decimal)Math.Sin(index * Math.PI / 6);

                var price = index == lastIndex
                    ? currentPrice
                    : Math.Round(currentPrice * (trendFactor + waveFactor) / 50m, MidpointRounding.AwayFromZero) * 50m;

                valuations.Add(new AssetValuation
                {
                    Id = new Guid($"{seriesIndex + 1:x8}-0000-4000-8000-{index + 1:x12}"),
                    AssetId = assetId,
                    ValuationDate = ValuationDates[index],
                    Price = price,
                    Source = DemonstrationSource,
                    IsDemonstrationData = true
                });
            }
        }

        return valuations;
    }

    private static DateOnly LastDayOfMonth(DateOnly firstDay)
    {
        return firstDay.AddMonths(1).AddDays(-1);
    }
}
