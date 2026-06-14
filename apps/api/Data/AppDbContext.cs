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
    }
}
