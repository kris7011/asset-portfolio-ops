using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AssetPortfolioOps.Api.Data;

/// <summary>
/// Brings the database to the latest schema with EF Core migrations.
/// </summary>
/// <remarks>
/// Earlier versions created the schema with <c>EnsureCreated</c>, which
/// records no migration history. Such a database already has the tables of
/// the <c>InitialCreate</c> migration, so that migration is marked as applied
/// (a baseline) before the remaining migrations run. Existing rows are kept.
/// </remarks>
public static class DatabaseInitializer
{
    public const string InitialMigrationId = "20261030134414_InitialCreate";

    public static void Initialize(AppDbContext dbContext)
    {
        if (dbContext.Database.IsRelational())
        {
            BaselineDatabaseCreatedWithoutMigrations(dbContext);
            dbContext.Database.Migrate();
        }
        else
        {
            // Non-relational test providers (EF Core InMemory) cannot migrate.
            dbContext.Database.EnsureCreated();
        }
    }

    private static void BaselineDatabaseCreatedWithoutMigrations(AppDbContext dbContext)
    {
        var connection = dbContext.Database.GetDbConnection();
        var openedHere = connection.State != System.Data.ConnectionState.Open;

        if (openedHere)
        {
            connection.Open();
        }

        try
        {
            if (!TableExists(connection, "Assets") || TableExists(connection, "__EFMigrationsHistory"))
            {
                return;
            }

            var historyRepository = dbContext.GetService<IHistoryRepository>();

            using var command = connection.CreateCommand();
            command.CommandText =
                historyRepository.GetCreateIfNotExistsScript() +
                historyRepository.GetInsertScript(new HistoryRow(InitialMigrationId, ProductVersion()));
            command.ExecuteNonQuery();
        }
        finally
        {
            if (openedHere)
            {
                connection.Close();
            }
        }
    }

    private static bool TableExists(System.Data.Common.DbConnection connection, string tableName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $name";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "$name";
        parameter.Value = tableName;
        command.Parameters.Add(parameter);

        return Convert.ToInt64(command.ExecuteScalar()) > 0;
    }

    private static string ProductVersion()
    {
        return typeof(DbContext).Assembly
            .GetName()
            .Version?
            .ToString(3) ?? "10.0.0";
    }
}
