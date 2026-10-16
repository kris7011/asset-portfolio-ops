using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace AssetPortfolioOps.Api.Tests.Api;

/// <summary>
/// Hosts the real API in memory against a private, shared-cache in-memory SQLite database.
/// The open connection keeps the database alive for the lifetime of the factory.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _keepAliveConnection = new(
        $"Data Source=asset-portfolio-ops-tests-{Guid.NewGuid():N};Mode=Memory;Cache=Shared");

    public ApiFactory()
    {
        _keepAliveConnection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _keepAliveConnection.ConnectionString);
        builder.UseSetting("CosmosDb:Enabled", "false");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _keepAliveConnection.Dispose();
        }
    }
}
