using AssetPortfolioOps.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace AssetPortfolioOps.Api.Features.Valuations;

public sealed record ValuationPoint(DateOnly Date, decimal Price);

public sealed record AssetValuationHistoryResponse(
    Guid AssetId,
    string AssetName,
    string Currency,
    bool IsDemonstrationData,
    IReadOnlyList<string> Sources,
    IReadOnlyList<ValuationPoint> Points,
    decimal? FirstPrice,
    decimal? LatestPrice,
    decimal? ChangeAmount,
    decimal? ChangePercent,
    decimal? MinPrice,
    decimal? MaxPrice);

public sealed record PortfolioValuationPoint(DateOnly Date, decimal TotalValue);

public sealed record PortfolioValuationHistoryResponse(
    Guid CustomerId,
    string CustomerName,
    string Currency,
    bool IsDemonstrationData,
    string Basis,
    IReadOnlyList<PortfolioValuationPoint> Points);

public interface IValuationService
{
    Task<AssetValuationHistoryResponse?> GetAssetHistoryAsync(
        Guid assetId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default);

    Task<PortfolioValuationHistoryResponse?> GetPortfolioHistoryAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);
}

public sealed class ValuationService(AppDbContext dbContext) : IValuationService
{
    public const string PortfolioBasis =
        "Current holdings valued at each historical price. Past holding quantities are not recorded, " +
        "so this shows how today's portfolio would have been valued, not its actual past value.";

    public async Task<AssetValuationHistoryResponse?> GetAssetHistoryAsync(
        Guid assetId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        if (from > to)
        {
            throw new ArgumentException("'from' must not be after 'to'.");
        }

        var asset = await dbContext.Assets
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == assetId, cancellationToken);

        if (asset is null)
        {
            return null;
        }

        var query = dbContext.AssetValuations
            .AsNoTracking()
            .Where(valuation => valuation.AssetId == assetId);

        if (from is not null)
        {
            query = query.Where(valuation => valuation.ValuationDate >= from);
        }

        if (to is not null)
        {
            query = query.Where(valuation => valuation.ValuationDate <= to);
        }

        // SQLite stores decimals as text, so ordering and aggregation happen in memory.
        var valuations = (await query.ToListAsync(cancellationToken))
            .OrderBy(valuation => valuation.ValuationDate)
            .ToList();

        var points = valuations
            .Select(valuation => new ValuationPoint(valuation.ValuationDate, valuation.Price))
            .ToList();

        decimal? first = points.Count > 0 ? points[0].Price : null;
        decimal? latest = points.Count > 0 ? points[^1].Price : null;
        decimal? change = latest - first;

        return new AssetValuationHistoryResponse(
            asset.Id,
            asset.Name,
            asset.Currency,
            valuations.Any(valuation => valuation.IsDemonstrationData),
            valuations.Select(valuation => valuation.Source).Distinct().ToList(),
            points,
            first,
            latest,
            change,
            first is > 0 ? Math.Round(change!.Value / first.Value * 100m, 2) : null,
            points.Count > 0 ? points.Min(point => point.Price) : null,
            points.Count > 0 ? points.Max(point => point.Price) : null);
    }

    public async Task<PortfolioValuationHistoryResponse?> GetPortfolioHistoryAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var customer = await dbContext.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == customerId, cancellationToken);

        if (customer is null)
        {
            return null;
        }

        var holdings = await dbContext.Holdings
            .AsNoTracking()
            .Where(holding => holding.CustomerId == customerId)
            .ToListAsync(cancellationToken);

        var assetIds = holdings.Select(holding => holding.AssetId).ToList();

        var valuations = await dbContext.AssetValuations
            .AsNoTracking()
            .Where(valuation => assetIds.Contains(valuation.AssetId))
            .ToListAsync(cancellationToken);

        var seriesByAsset = valuations
            .GroupBy(valuation => valuation.AssetId)
            .ToDictionary(group => group.Key, group => group.OrderBy(valuation => valuation.ValuationDate).ToList());

        var points = new List<PortfolioValuationPoint>();

        foreach (var date in valuations.Select(valuation => valuation.ValuationDate).Distinct().Order())
        {
            decimal total = 0m;
            var complete = true;

            foreach (var holding in holdings)
            {
                // The latest price on or before the date; none yet means the
                // portfolio cannot be valued on that date.
                var price = seriesByAsset.TryGetValue(holding.AssetId, out var series)
                    ? series.LastOrDefault(valuation => valuation.ValuationDate <= date)?.Price
                    : null;

                if (price is null)
                {
                    complete = false;
                    break;
                }

                total += holding.Quantity * price.Value;
            }

            if (complete && holdings.Count > 0)
            {
                points.Add(new PortfolioValuationPoint(date, total));
            }
        }

        return new PortfolioValuationHistoryResponse(
            customer.Id,
            customer.Name,
            "DKK",
            valuations.Any(valuation => valuation.IsDemonstrationData),
            PortfolioBasis,
            points);
    }
}
