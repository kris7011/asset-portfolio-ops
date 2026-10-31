using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AssetPortfolioOps.Api.Data;

namespace AssetPortfolioOps.Api.Tests.Api;

/// <summary>
/// Valuation history and purchase preview through HTTP, on SQLite created by the EF Core migrations.
/// </summary>
public sealed class ValuationAndPreviewApiTests : IDisposable
{
    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public ValuationAndPreviewApiTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task AssetValuations_ReturnTheLabelledDemonstrationSeries()
    {
        var history = await _client.GetFromJsonAsync<JsonElement>(
            $"/api/assets/{SeedData.BordeauxAssetId}/valuations");

        Assert.Equal(24, history.GetProperty("points").GetArrayLength());
        Assert.True(history.GetProperty("isDemonstrationData").GetBoolean());
        Assert.Equal(ValuationSeedData.DemonstrationSource, history.GetProperty("sources")[0].GetString());
        Assert.Equal(12_500m, history.GetProperty("latestPrice").GetDecimal());
        Assert.Equal("2024-10-31", history.GetProperty("points")[0].GetProperty("date").GetString());
    }

    [Fact]
    public async Task AssetValuations_FilterByDate_AndValidateTheRange()
    {
        var filtered = await _client.GetFromJsonAsync<JsonElement>(
            $"/api/assets/{SeedData.BordeauxAssetId}/valuations?from=2026-07-01&to=2026-09-30");

        Assert.Equal(3, filtered.GetProperty("points").GetArrayLength());

        var inverted = await _client.GetAsync(
            $"/api/assets/{SeedData.BordeauxAssetId}/valuations?from=2026-09-30&to=2026-07-01");

        Assert.Equal(HttpStatusCode.BadRequest, inverted.StatusCode);

        var unknown = await _client.GetAsync($"/api/assets/{Guid.NewGuid()}/valuations");

        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }

    [Fact]
    public async Task PortfolioValuationHistory_EndsAtTheCurrentPortfolioValue()
    {
        var history = await _client.GetFromJsonAsync<JsonElement>(
            $"/api/customers/{SeedData.CustomerEmmaId}/portfolio/valuation-history");

        var points = history.GetProperty("points");

        Assert.Equal(24, points.GetArrayLength());
        Assert.Equal(101_700m, points[points.GetArrayLength() - 1].GetProperty("totalValue").GetDecimal());
        Assert.Contains("not recorded", history.GetProperty("basis").GetString());
    }

    [Fact]
    public async Task PurchasePreview_ReturnsBeforeAndAfter_AndChangesNothing()
    {
        var holdingsBefore = await _client.GetStringAsync($"/api/customers/{SeedData.CustomerEmmaId}/portfolio");
        var inventoryBefore = await _client.GetStringAsync("/api/inventory");

        var preview = await _client.GetFromJsonAsync<JsonElement>(
            $"/api/customers/{SeedData.CustomerEmmaId}/purchase-preview?assetId={SeedData.BordeauxAssetId}&quantity=2&requestedPrice=12300");

        Assert.True(preview.GetProperty("isPreview").GetBoolean());
        Assert.Equal(73.75m, preview.GetProperty("before").GetProperty("largestSharePercent").GetDecimal());
        Assert.Equal(78.93m, preview.GetProperty("after").GetProperty("largestSharePercent").GetDecimal());
        Assert.Equal(24_600m, preview.GetProperty("estimatedCost").GetDecimal());
        Assert.Equal("Requested", preview.GetProperty("priceSource").GetString());

        // Nothing was committed: no request, no audit event, same holdings and inventory.
        Assert.Equal(0, (await _client.GetFromJsonAsync<JsonElement>("/api/purchase-requests")).GetArrayLength());
        Assert.Equal(0, (await _client.GetFromJsonAsync<JsonElement>("/api/audit-events")).GetArrayLength());
        Assert.Equal(holdingsBefore, await _client.GetStringAsync($"/api/customers/{SeedData.CustomerEmmaId}/portfolio"));
        Assert.Equal(inventoryBefore, await _client.GetStringAsync("/api/inventory"));
    }

    [Theory]
    [InlineData("quantity=0", HttpStatusCode.BadRequest)]
    [InlineData("quantity=2&requestedPrice=-1", HttpStatusCode.BadRequest)]
    [InlineData("quantity=abc", HttpStatusCode.BadRequest)]
    public async Task PurchasePreview_RejectsInvalidInput(string query, HttpStatusCode expected)
    {
        var response = await _client.GetAsync(
            $"/api/customers/{SeedData.CustomerEmmaId}/purchase-preview?assetId={SeedData.BordeauxAssetId}&{query}");

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task PurchasePreview_ReturnsNotFoundForUnknownCustomerOrAsset()
    {
        var unknownCustomer = await _client.GetAsync(
            $"/api/customers/{Guid.NewGuid()}/purchase-preview?assetId={SeedData.BordeauxAssetId}&quantity=1");
        var unknownAsset = await _client.GetAsync(
            $"/api/customers/{SeedData.CustomerEmmaId}/purchase-preview?assetId={Guid.NewGuid()}&quantity=1");

        Assert.Equal(HttpStatusCode.NotFound, unknownCustomer.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknownAsset.StatusCode);
    }

    [Fact]
    public async Task CreatingARequestAfterAPreview_IsASeparateExplicitStep()
    {
        await _client.GetAsync(
            $"/api/customers/{SeedData.CustomerEmmaId}/purchase-preview?assetId={SeedData.BordeauxAssetId}&quantity=2");

        var created = await _client.PostAsJsonAsync("/api/purchase-requests", new
        {
            customerId = SeedData.CustomerEmmaId,
            assetId = SeedData.BordeauxAssetId,
            quantity = 2,
            requestedPrice = 12_300m,
            requestedBy = "Kris"
        });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(1, (await _client.GetFromJsonAsync<JsonElement>("/api/purchase-requests")).GetArrayLength());
        Assert.Equal(1, (await _client.GetFromJsonAsync<JsonElement>("/api/audit-events")).GetArrayLength());
    }
}
