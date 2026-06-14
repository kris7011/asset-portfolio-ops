using AssetPortfolioOps.Api.Domain;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace AssetPortfolioOps.Api.Features.AuditEvents;

public sealed class CosmosAuditEventStore(
    CosmosClient cosmosClient,
    IOptions<CosmosDbOptions> options) : IAuditEventStore
{
    private readonly CosmosDbOptions _options = options.Value;
    private Container? _container;

    public async Task AddAsync(AuditEvent auditEvent)
    {
        var container = await GetContainerAsync();
        var document = CosmosAuditEventDocument.FromDomain(auditEvent);

        await container.CreateItemAsync(
            document,
            new PartitionKey(document.EntityId));
    }

    public async Task<IReadOnlyCollection<AuditEvent>> GetAllAsync()
    {
        var container = await GetContainerAsync();

        var query = new QueryDefinition(
            "SELECT * FROM c ORDER BY c.timestampUtc DESC");

        var iterator = container.GetItemQueryIterator<CosmosAuditEventDocument>(query);
        var documents = new List<CosmosAuditEventDocument>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            documents.AddRange(response);
        }

        return documents
            .Select(document => document.ToDomain())
            .ToList();
    }

    private async Task<Container> GetContainerAsync()
    {
        if (_container is not null)
        {
            return _container;
        }

        var databaseResponse = await cosmosClient.CreateDatabaseIfNotExistsAsync(
            _options.DatabaseName);

        var containerResponse = await databaseResponse.Database.CreateContainerIfNotExistsAsync(
            _options.AuditEventsContainerName,
            "/entityId");

        _container = containerResponse.Container;

        return _container;
    }
}
