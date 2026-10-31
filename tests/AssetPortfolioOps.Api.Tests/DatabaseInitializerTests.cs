using AssetPortfolioOps.Api.Data;
using AssetPortfolioOps.Api.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AssetPortfolioOps.Api.Tests;

/// <summary>
/// Upgrading SQLite databases on disk, as a developer's local database would be.
/// </summary>
public sealed class DatabaseInitializerTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("apo-db-").FullName;

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(_directory, recursive: true);
    }

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={Path.Combine(_directory, "app.db")}")
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public void Initialize_CreatesANewDatabaseWithAllMigrationsAndTheDemonstrationSeries()
    {
        using var dbContext = CreateContext();

        DatabaseInitializer.Initialize(dbContext);

        Assert.Empty(dbContext.Database.GetPendingMigrations());
        Assert.Equal(ValuationSeedData.Valuations.Count, dbContext.AssetValuations.Count());
        Assert.Equal(SeedData.Assets.Count, dbContext.Assets.Count());
    }

    [Fact]
    public void Initialize_UpgradesADatabaseCreatedByEnsureCreated_AndKeepsItsData()
    {
        var requestId = Guid.NewGuid();

        // The previous version: the InitialCreate schema without migration history.
        using (var legacy = CreateContext())
        {
            legacy.GetService<IMigrator>().Migrate(DatabaseInitializer.InitialMigrationId);
            legacy.Database.ExecuteSqlRaw("DROP TABLE \"__EFMigrationsHistory\"");

            legacy.PurchaseRequests.Add(new PurchaseRequest
            {
                Id = requestId,
                CustomerId = SeedData.CustomerEmmaId,
                AssetId = SeedData.BordeauxAssetId,
                Quantity = 1,
                RequestedPrice = 12_000m,
                Status = PurchaseRequestStatus.Approved,
                RequestedBy = "Kris",
                CreatedUtc = new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc)
            });
            legacy.SaveChanges();
        }

        using var dbContext = CreateContext();

        DatabaseInitializer.Initialize(dbContext);

        Assert.Empty(dbContext.Database.GetPendingMigrations());
        Assert.Equal(PurchaseRequestStatus.Approved, dbContext.PurchaseRequests.Single(request => request.Id == requestId).Status);
        Assert.Equal(ValuationSeedData.Valuations.Count, dbContext.AssetValuations.Count());
    }

    [Fact]
    public void Initialize_IsIdempotent()
    {
        using (var first = CreateContext())
        {
            DatabaseInitializer.Initialize(first);
        }

        using var dbContext = CreateContext();

        DatabaseInitializer.Initialize(dbContext);

        Assert.Equal(2, dbContext.Database.GetAppliedMigrations().Count());
        Assert.Equal(ValuationSeedData.Valuations.Count, dbContext.AssetValuations.Count());
    }
}
