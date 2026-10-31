using AssetPortfolioOps.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AssetPortfolioOps.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Holding> Holdings => Set<Holding>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<PurchaseRequest> PurchaseRequests => Set<PurchaseRequest>();
    public DbSet<AssetValuation> AssetValuations => Set<AssetValuation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>().HasData(SeedData.Customers);
        modelBuilder.Entity<Asset>().HasData(SeedData.Assets);
        modelBuilder.Entity<Holding>().HasData(SeedData.Holdings);
        modelBuilder.Entity<InventoryItem>().HasData(SeedData.InventoryItems);

        modelBuilder.Entity<Asset>()
            .Property(asset => asset.MarketPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Holding>()
            .Property(holding => holding.AveragePurchasePrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<PurchaseRequest>()
            .Property(request => request.RequestedPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<AssetValuation>(valuation =>
        {
            valuation.Property(item => item.Price).HasPrecision(18, 2);
            valuation.Property(item => item.Source).HasMaxLength(200);

            // One price per asset and day.
            valuation.HasIndex(item => new { item.AssetId, item.ValuationDate }).IsUnique();

            valuation.HasOne<Asset>()
                .WithMany()
                .HasForeignKey(item => item.AssetId)
                .OnDelete(DeleteBehavior.Cascade);

            valuation.HasData(ValuationSeedData.Valuations);
        });
    }
}
