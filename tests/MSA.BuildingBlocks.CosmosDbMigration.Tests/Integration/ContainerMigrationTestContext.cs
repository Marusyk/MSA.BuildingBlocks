using System.Dynamic;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace MSA.BuildingBlocks.CosmosDbMigration.Tests;

public sealed class ContainerMigrationTestContext : IAsyncDisposable
{
    private const string DatabaseId = "TestDb";
    private const string ConnectionString =
        "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

    private readonly CosmosClient _client;

    public ContainerMigration ContainerMigration { get; }

    private ContainerMigrationTestContext(
        CosmosClient client,
        ContainerMigration containerMigration)
    {
        _client = client;
        ContainerMigration = containerMigration;
    }

    public static async Task<ContainerMigrationTestContext> CreateAsync(IList<ExpandoObject> seedItems)
    {
        CosmosClient client = new(ConnectionString);
        Database db = await client.CreateDatabaseAsync(DatabaseId);

        string containerId = $"TestContainer_{Guid.NewGuid()}";
        await db.CreateContainerAsync(new ContainerProperties(containerId, "/CountryCode"));

        ILogger<ContainerMigration> logger = new LoggerFactory()
            .CreateLogger<ContainerMigration>();

        ContainerMigration migration = new(client, DatabaseId, containerId, logger);

        if (seedItems.Count > 0)
        {
            await migration.UpsertItems(seedItems);
        }

        return new ContainerMigrationTestContext(client, migration);
    }

    public async ValueTask DisposeAsync()
    {
        await _client.GetDatabase(DatabaseId).DeleteAsync();
        _client.Dispose();
    }
}
