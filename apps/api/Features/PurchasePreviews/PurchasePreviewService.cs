using static System.FormattableString;
using AssetPortfolioOps.Api.Data;
using AssetPortfolioOps.Api.Domain;
using AssetPortfolioOps.Api.Features.Portfolios;
using Microsoft.EntityFrameworkCore;

namespace AssetPortfolioOps.Api.Features.PurchasePreviews;

public sealed record PreviewAsset(Guid AssetId, string AssetName, decimal MarketPrice, string Currency);

public sealed record PreviewInventory(int QuantityAvailable, bool IsSufficient);

public sealed record PreviewPendingRequests(int Count, decimal Exposure);

public sealed record ConcentrationSnapshot(
    decimal TotalMarketValue,
    IReadOnlyList<PositionShare> Positions,
    string? LargestAssetName,
    decimal LargestSharePercent,
    string Level);

public sealed record PreviewWarning(string Code, string Severity, string Message);

public sealed record PurchasePreviewResponse(
    bool IsPreview,
    string Notice,
    Guid CustomerId,
    string CustomerName,
    PreviewAsset Asset,
    int Quantity,
    decimal PricePerUnit,
    string PriceSource,
    decimal EstimatedCost,
    PreviewInventory Inventory,
    PreviewPendingRequests PendingRequests,
    ConcentrationSnapshot Before,
    ConcentrationSnapshot After,
    decimal LargestShareChangePercentagePoints,
    IReadOnlyList<PreviewWarning> Warnings);

public interface IPurchasePreviewService
{
    /// <returns>Null when the customer or the asset does not exist.</returns>
    Task<PurchasePreviewResponse?> PreviewAsync(
        Guid customerId,
        Guid assetId,
        int quantity,
        decimal? requestedPrice,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Shows what a purchase would do to a customer's portfolio before anything
/// is committed. It only reads: no purchase request, holding, inventory change
/// or audit event is created, and the database context tracks nothing.
/// </summary>
public sealed class PurchasePreviewService(AppDbContext dbContext) : IPurchasePreviewService
{
    public const int MaxQuantity = 10_000;

    public const decimal PriceDeviationWarningPercent = 10m;

    public const string PreviewNotice =
        "Preview only. Nothing has been saved: no purchase request, holding, inventory change or audit event was created.";

    public async Task<PurchasePreviewResponse?> PreviewAsync(
        Guid customerId,
        Guid assetId,
        int quantity,
        decimal? requestedPrice,
        CancellationToken cancellationToken = default)
    {
        if (quantity is <= 0 or > MaxQuantity)
        {
            throw new ArgumentException($"Quantity must be between 1 and {MaxQuantity}.");
        }

        if (requestedPrice is <= 0)
        {
            throw new ArgumentException("Requested price must be greater than zero.");
        }

        var customer = await dbContext.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == customerId, cancellationToken);

        var asset = await dbContext.Assets
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == assetId, cancellationToken);

        if (customer is null || asset is null)
        {
            return null;
        }

        var holdings = await dbContext.Holdings
            .AsNoTracking()
            .Where(holding => holding.CustomerId == customerId)
            .Join(
                dbContext.Assets.AsNoTracking(),
                holding => holding.AssetId,
                item => item.Id,
                (holding, item) => new
                {
                    item.Id,
                    item.Name,
                    holding.Quantity,
                    item.MarketPrice
                })
            .ToListAsync(cancellationToken);

        var currentPositions = holdings
            .Select(holding => new PositionValue(
                holding.Id,
                holding.Name,
                holding.Quantity,
                holding.Quantity * holding.MarketPrice))
            .ToList();

        // Positions are valued at market price; the price paid only affects the cost.
        var purchasedPosition = new PositionValue(asset.Id, asset.Name, quantity, quantity * asset.MarketPrice);

        var before = PortfolioConcentration.Calculate(currentPositions);
        var after = PortfolioConcentration.Calculate(currentPositions.Append(purchasedPosition));

        var quantityAvailable = await dbContext.InventoryItems
            .AsNoTracking()
            .Where(item => item.AssetId == assetId)
            .SumAsync(item => item.QuantityAvailable, cancellationToken);

        var pending = (await dbContext.PurchaseRequests
                .AsNoTracking()
                .Where(request =>
                    request.CustomerId == customerId &&
                    request.Status == PurchaseRequestStatus.Pending)
                .Select(request => new { request.Quantity, request.RequestedPrice })
                .ToListAsync(cancellationToken))
            .ToList();

        var pricePerUnit = requestedPrice ?? asset.MarketPrice;
        var inventory = new PreviewInventory(quantityAvailable, quantity <= quantityAvailable);
        var pendingRequests = new PreviewPendingRequests(
            pending.Count,
            pending.Sum(request => request.Quantity * request.RequestedPrice));

        return new PurchasePreviewResponse(
            IsPreview: true,
            Notice: PreviewNotice,
            CustomerId: customer.Id,
            CustomerName: customer.Name,
            Asset: new PreviewAsset(asset.Id, asset.Name, asset.MarketPrice, asset.Currency),
            Quantity: quantity,
            PricePerUnit: pricePerUnit,
            PriceSource: requestedPrice is null ? "Market" : "Requested",
            EstimatedCost: quantity * pricePerUnit,
            Inventory: inventory,
            PendingRequests: pendingRequests,
            Before: ToSnapshot(before),
            After: ToSnapshot(after),
            LargestShareChangePercentagePoints: Round(after.LargestSharePercent - before.LargestSharePercent),
            Warnings: BuildWarnings(before, after, asset, quantity, pricePerUnit, inventory, pendingRequests));
    }

    private static List<PreviewWarning> BuildWarnings(
        ConcentrationResult before,
        ConcentrationResult after,
        Asset asset,
        int quantity,
        decimal pricePerUnit,
        PreviewInventory inventory,
        PreviewPendingRequests pendingRequests)
    {
        var warnings = new List<PreviewWarning>();
        var afterName = after.LargestPosition?.AssetName ?? asset.Name;
        var afterShare = Round(after.LargestSharePercent);

        if (PortfolioConcentration.Rank(after.Level) > PortfolioConcentration.Rank(before.Level))
        {
            warnings.Add(new PreviewWarning(
                "ConcentrationIncrease",
                after.Level,
                Invariant($"Concentration would rise from {before.Level} to {after.Level}: {afterName} would be {afterShare}% of the portfolio.")));
        }
        else if (PortfolioConcentration.Rank(after.Level) < PortfolioConcentration.Rank(before.Level))
        {
            warnings.Add(new PreviewWarning(
                "ConcentrationDecrease",
                "Low",
                Invariant($"Concentration would fall from {before.Level} to {after.Level}: the largest position ({afterName}) would be {afterShare}% of the portfolio (now {Round(before.LargestSharePercent)}%).")));
        }
        else if (after.Level != "None")
        {
            warnings.Add(new PreviewWarning(
                "ConcentrationRemains",
                after.Level,
                Invariant($"Concentration stays {after.Level}: {afterName} would be {afterShare}% of the portfolio (now {Round(before.LargestSharePercent)}%).")));
        }

        if (!inventory.IsSufficient)
        {
            warnings.Add(new PreviewWarning(
                "InsufficientInventory",
                "High",
                Invariant($"Only {inventory.QuantityAvailable} units of {asset.Name} are in inventory; {quantity} requested.")));
        }

        var deviation = asset.MarketPrice > 0
            ? (pricePerUnit - asset.MarketPrice) / asset.MarketPrice * 100m
            : 0m;

        if (Math.Abs(deviation) > PriceDeviationWarningPercent)
        {
            warnings.Add(new PreviewWarning(
                "PriceDeviation",
                "Medium",
                Invariant($"The price per unit differs from the market price by {Round(deviation)}%.")));
        }

        if (pendingRequests.Count > 0)
        {
            warnings.Add(new PreviewWarning(
                "PendingRequests",
                "Low",
                Invariant($"{pendingRequests.Count} pending purchase request(s) for this customer ({Math.Round(pendingRequests.Exposure, 0)} DKK) are not included in this preview.")));
        }

        return warnings;
    }

    private static ConcentrationSnapshot ToSnapshot(ConcentrationResult result)
    {
        return new ConcentrationSnapshot(
            result.TotalMarketValue,
            result.Positions
                .Select(position => position with { SharePercent = Round(position.SharePercent) })
                .ToList(),
            result.LargestPosition?.AssetName,
            Round(result.LargestSharePercent),
            result.Level);
    }

    private static decimal Round(decimal value)
    {
        return Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
