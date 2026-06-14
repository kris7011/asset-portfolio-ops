using AssetPortfolioOps.Api.Data;
using AssetPortfolioOps.Api.Features.Assets;
using AssetPortfolioOps.Api.Features.AuditEvents;
using AssetPortfolioOps.Api.Features.Inventory;
using AssetPortfolioOps.Api.Features.Portfolios;
using AssetPortfolioOps.Api.Features.PurchaseRequests;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=asset-portfolio-ops.db";

    options.UseSqlite(connectionString);
});

builder.Services.Configure<CosmosDbOptions>(
    builder.Configuration.GetSection("CosmosDb"));

builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IPortfolioService, PortfolioService>();
builder.Services.AddScoped<IPurchaseRequestService, PurchaseRequestService>();

var cosmosDbEnabled = builder.Configuration.GetValue<bool>("CosmosDb:Enabled");

if (cosmosDbEnabled)
{
    builder.Services.AddSingleton(serviceProvider =>
    {
        var cosmosOptions = serviceProvider
            .GetRequiredService<IOptions<CosmosDbOptions>>()
            .Value;

        if (string.IsNullOrWhiteSpace(cosmosOptions.Endpoint))
        {
            throw new InvalidOperationException("CosmosDb:Endpoint is required when CosmosDb:Enabled is true.");
        }

        if (string.IsNullOrWhiteSpace(cosmosOptions.Key))
        {
            throw new InvalidOperationException("CosmosDb:Key is required when CosmosDb:Enabled is true.");
        }

        return new CosmosClient(
            cosmosOptions.Endpoint,
            cosmosOptions.Key,
            new CosmosClientOptions
            {
                ApplicationName = "AssetPortfolioOps.Api"
            });
    });

    builder.Services.AddSingleton<IAuditEventStore, CosmosAuditEventStore>();
}
else
{
    builder.Services.AddSingleton<IAuditEventStore, InMemoryAuditEventStore>();
}

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("LocalFrontend");

app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        status = "Healthy",
        application = "AssetPortfolioOps.Api",
        cosmosDbEnabled
    });
});

var api = app.MapGroup("/api");

api.MapGet("/assets", (IAssetService assetService) =>
{
    return Results.Ok(assetService.GetAll());
});

api.MapGet("/assets/{id:guid}", (Guid id, IAssetService assetService) =>
{
    var asset = assetService.GetById(id);

    return asset is null
        ? Results.NotFound()
        : Results.Ok(asset);
});

api.MapGet("/customers/{customerId:guid}/portfolio", (
    Guid customerId,
    IPortfolioService portfolioService) =>
{
    var portfolio = portfolioService.GetPortfolio(customerId);

    return portfolio is null
        ? Results.NotFound()
        : Results.Ok(portfolio);
});

api.MapGet("/inventory", (IInventoryService inventoryService) =>
{
    return Results.Ok(inventoryService.GetAll());
});

api.MapGet("/purchase-requests", (IPurchaseRequestService purchaseRequestService) =>
{
    return Results.Ok(purchaseRequestService.GetAll());
});

api.MapPost("/purchase-requests", async (
    CreatePurchaseRequestRequest request,
    IPurchaseRequestService purchaseRequestService) =>
{
    try
    {
        var createdRequest = await purchaseRequestService.CreateAsync(request);

        return Results.Created($"/api/purchase-requests/{createdRequest.Id}", createdRequest);
    }
    catch (InvalidOperationException exception)
    {
        return Results.BadRequest(new
        {
            error = exception.Message
        });
    }
});

api.MapGet("/audit-events", async (IAuditEventStore auditEventStore) =>
{
    return Results.Ok(await auditEventStore.GetAllAsync());
});

app.Run();

public partial class Program
{
}
