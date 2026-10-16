using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AssetPortfolioOps.Api.Data;

namespace AssetPortfolioOps.Api.Tests.Api;

/// <summary>
/// End-to-end tests through HTTP, routing, model binding, EF Core on SQLite and JSON serialization.
/// Each test gets its own API instance and database.
/// </summary>
public sealed class ApiIntegrationTests : IDisposable
{
    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public ApiIntegrationTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var health = await _client.GetFromJsonAsync<JsonElement>("/health");

        Assert.Equal("Healthy", health.GetProperty("status").GetString());
        Assert.False(health.GetProperty("cosmosDbEnabled").GetBoolean());
    }

    [Fact]
    public async Task GetAssets_ReturnsSeededAssets()
    {
        var assets = await _client.GetFromJsonAsync<JsonElement>("/api/assets");

        Assert.Equal(SeedData.Assets.Count, assets.GetArrayLength());
    }

    [Fact]
    public async Task GetAsset_ReturnsNotFound_WhenAssetDoesNotExist()
    {
        var response = await _client.GetAsync($"/api/assets/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPortfolio_ReturnsCalculatedValues_FromSqlite()
    {
        var portfolio = await _client.GetFromJsonAsync<JsonElement>(
            $"/api/customers/{SeedData.CustomerEmmaId}/portfolio");

        // 6 x 12,500 (Bordeaux) + 3 x 8,900 (Burgundy)
        Assert.Equal(101_700m, portfolio.GetProperty("totalMarketValue").GetDecimal());
        // (12,500 - 9,800) x 6 + (8,900 - 7,600) x 3
        Assert.Equal(20_100m, portfolio.GetProperty("totalGainLoss").GetDecimal());
        Assert.Equal(2, portfolio.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task GetPortfolio_ReturnsNotFound_WhenCustomerDoesNotExist()
    {
        var response = await _client.GetAsync($"/api/customers/{Guid.NewGuid()}/portfolio");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetRiskIndicators_ReturnsConcentrationAndInventoryWarnings()
    {
        var indicators = await _client.GetFromJsonAsync<JsonElement>(
            $"/api/customers/{SeedData.CustomerEmmaId}/risk-indicators");

        var types = indicators.EnumerateArray()
            .Select(indicator => indicator.GetProperty("type").GetString())
            .ToList();

        Assert.Contains("HighConcentration", types);
        Assert.Contains("LowInventory", types);
        Assert.DoesNotContain("PendingRequestExposure", types);
    }

    [Fact]
    public async Task CreatePurchaseRequest_ReturnsBadRequestWithMessage_WhenQuantityIsZero()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/purchase-requests",
            NewPurchaseRequest(quantity: 0));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Quantity must be greater than zero.", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task PurchaseRequestWorkflow_CreateApproveComplete_UpdatesStatusAndAuditTrail()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/purchase-requests", NewPurchaseRequest());

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetGuid();
        Assert.Equal($"/api/purchase-requests/{id}", createResponse.Headers.Location?.OriginalString);
        // The frontend relies on the status being serialized as a number (Pending = 0).
        Assert.Equal(0, created.GetProperty("status").GetInt32());

        var approved = await PatchStatusAsync(id, "approve");
        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
        Assert.Equal(1, (await approved.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetInt32());

        var completed = await PatchStatusAsync(id, "complete");
        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);
        Assert.Equal(3, (await completed.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetInt32());

        var auditEvents = await _client.GetFromJsonAsync<JsonElement>("/api/audit-events");
        var actions = auditEvents.EnumerateArray()
            .Where(auditEvent => auditEvent.GetProperty("entityId").GetString() == id.ToString())
            .Select(auditEvent => auditEvent.GetProperty("action").GetString())
            .ToList();

        Assert.Equal(["Completed", "Approved", "Created"], actions);
    }

    [Fact]
    public async Task CompletePurchaseRequest_ReturnsBadRequest_WhenRequestIsStillPending()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/purchase-requests", NewPurchaseRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();

        var response = await PatchStatusAsync(created.GetProperty("id").GetGuid(), "complete");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(
            "Cannot change purchase request status from Pending to Completed.",
            body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task ApprovePurchaseRequest_ReturnsNotFound_WhenRequestDoesNotExist()
    {
        var response = await PatchStatusAsync(Guid.NewGuid(), "approve");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreatePurchaseRequest_AddsPendingExposureRiskIndicator()
    {
        await _client.PostAsJsonAsync(
            "/api/purchase-requests",
            NewPurchaseRequest(quantity: 3, requestedPrice: 12_300m));

        var indicators = await _client.GetFromJsonAsync<JsonElement>(
            $"/api/customers/{SeedData.CustomerEmmaId}/risk-indicators");

        var exposure = indicators.EnumerateArray()
            .Single(indicator => indicator.GetProperty("type").GetString() == "PendingRequestExposure");
        // 3 x 12,300 = 36,900 DKK, which is in the Medium band (25,000-49,999)
        Assert.Equal("Medium", exposure.GetProperty("severity").GetString());
    }

    private Task<HttpResponseMessage> PatchStatusAsync(Guid id, string action)
    {
        return _client.PatchAsJsonAsync(
            $"/api/purchase-requests/{id}/{action}",
            new { performedBy = "Katrine" });
    }

    private static object NewPurchaseRequest(int quantity = 2, decimal requestedPrice = 12_300m)
    {
        return new
        {
            customerId = SeedData.CustomerEmmaId,
            assetId = SeedData.BordeauxAssetId,
            quantity,
            requestedPrice,
            requestedBy = "Kris"
        };
    }
}
