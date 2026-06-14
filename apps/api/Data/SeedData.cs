using AssetPortfolioOps.Api.Domain;

namespace AssetPortfolioOps.Api.Data;

public static class SeedData
{
    public static readonly Guid CustomerEmmaId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid CustomerNoahId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static readonly Guid BordeauxAssetId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid BurgundyAssetId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public static readonly Guid ChampagneAssetId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    public static IReadOnlyList<Customer> Customers { get; } = new List<Customer>
    {
        new()
        {
            Id = CustomerEmmaId,
            Name = "Emma Jensen",
            Email = "emma.jensen@example.com"
        },
        new()
        {
            Id = CustomerNoahId,
            Name = "Noah Madsen",
            Email = "noah.madsen@example.com"
        }
    };

    public static IReadOnlyList<Asset> Assets { get; } = new List<Asset>
    {
        new()
        {
            Id = BordeauxAssetId,
            Name = "Bordeaux Premier Cru 2016",
            Type = AssetType.FineWine,
            Region = "Bordeaux",
            VintageYear = 2016,
            MarketPrice = 12500m,
            Currency = "DKK"
        },
        new()
        {
            Id = BurgundyAssetId,
            Name = "Burgundy Grand Cru 2019",
            Type = AssetType.FineWine,
            Region = "Burgundy",
            VintageYear = 2019,
            MarketPrice = 8900m,
            Currency = "DKK"
        },
        new()
        {
            Id = ChampagneAssetId,
            Name = "Vintage Champagne 2012",
            Type = AssetType.FineWine,
            Region = "Champagne",
            VintageYear = 2012,
            MarketPrice = 4200m,
            Currency = "DKK"
        }
    };

    public static IReadOnlyList<Holding> Holdings { get; } = new List<Holding>
    {
        new()
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            CustomerId = CustomerEmmaId,
            AssetId = BordeauxAssetId,
            Quantity = 6,
            AveragePurchasePrice = 9800m
        },
        new()
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            CustomerId = CustomerEmmaId,
            AssetId = BurgundyAssetId,
            Quantity = 3,
            AveragePurchasePrice = 7600m
        },
        new()
        {
            Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
            CustomerId = CustomerNoahId,
            AssetId = ChampagneAssetId,
            Quantity = 12,
            AveragePurchasePrice = 3500m
        }
    };

    public static IReadOnlyList<InventoryItem> InventoryItems { get; } = new List<InventoryItem>
    {
        new()
        {
            Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
            AssetId = BordeauxAssetId,
            QuantityAvailable = 18,
            WarehouseLocation = "AAL-WH-01",
            LastUpdatedUtc = new DateTime(2026, 6, 14, 8, 0, 0, DateTimeKind.Utc)
        },
        new()
        {
            Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
            AssetId = BurgundyAssetId,
            QuantityAvailable = 9,
            WarehouseLocation = "AAL-WH-01",
            LastUpdatedUtc = new DateTime(2026, 6, 14, 8, 0, 0, DateTimeKind.Utc)
        },
        new()
        {
            Id = Guid.Parse("88888888-8888-8888-8888-888888888888"),
            AssetId = ChampagneAssetId,
            QuantityAvailable = 30,
            WarehouseLocation = "AAL-WH-02",
            LastUpdatedUtc = new DateTime(2026, 6, 14, 8, 0, 0, DateTimeKind.Utc)
        }
    };
}
