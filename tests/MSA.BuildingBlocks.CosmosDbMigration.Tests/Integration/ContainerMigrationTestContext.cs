using System.Dynamic;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace MSA.BuildingBlocks.CosmosDbMigration.Tests.Integration;

public sealed class ContainerMigrationTestContext : IAsyncDisposable
{
    private const string ConnectionString =
        "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
    private static string _databaseId;

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
        _databaseId = $"TestDb_{Guid.NewGuid()}";
        Database db = await client.CreateDatabaseAsync(_databaseId);

        string containerId = $"TestContainer_{Guid.NewGuid()}";
        await db.CreateContainerAsync(new ContainerProperties(containerId, "/CountryCode"));

        ILogger<ContainerMigration> logger = new LoggerFactory()
            .CreateLogger<ContainerMigration>();

        ContainerMigration migration = new(client, _databaseId, containerId, logger);

        if (seedItems.Count > 0)
        {
            await migration.UpsertItems(seedItems);
        }

        return new ContainerMigrationTestContext(client, migration);
    }

    public async ValueTask DisposeAsync()
    {
        await _client.GetDatabase(_databaseId).DeleteAsync();
        _client.Dispose();
    }
}
