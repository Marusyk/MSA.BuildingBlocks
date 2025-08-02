using System.Dynamic;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace MSA.BuildingBlocks.CosmosDbMigration.Tests.Integration;

public sealed class MigrationTestContext<TMigration> : IAsyncDisposable
{
    private const string ConnectionString =
        "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
    private static string _databaseId;
    private readonly CosmosClient _client;

    public TMigration Migration { get; }
    public Database Database { get; }
    public Container Container { get; }
    public string PartitionKey { get; }

    private MigrationTestContext(
        CosmosClient client,
        Database database,
        Container container,
        string partitionKey,
        TMigration migration)
    {
        _client = client;
        Database = database;
        Container = container;
        PartitionKey = partitionKey;
        Migration = migration;
    }

    public static async Task<MigrationTestContext<TMigration>> CreateAsync(
        Func<CosmosClient, string, string, ILogger<TMigration>, TMigration> migrationFactory,
        IList<ExpandoObject> seedItems)
    {

        CosmosClient client = new(ConnectionString);
        _databaseId = $"TestDb_{Guid.NewGuid()}";
        Database database = await client.CreateDatabaseAsync(_databaseId);

        string containerId = $"TestContainer_{Guid.NewGuid()}";
        string partitionKey = "CountryCode";
        ContainerProperties containerProps = new(containerId, $"/{partitionKey}");
        Container container = await database.CreateContainerAsync(containerProps);

        ILogger<TMigration> logger = new LoggerFactory().CreateLogger<TMigration>();
        TMigration migration = migrationFactory(client, _databaseId, containerId, logger);

        await SeedDbIfDataProvided(seedItems, client, containerId);

        return new MigrationTestContext<TMigration>(
            client,
            database,
            container,
            partitionKey,
            migration);
    }

    public async ValueTask DisposeAsync()
    {
        await _client.GetDatabase(_databaseId).DeleteAsync();
        _client.Dispose();
    }

    private static async Task SeedDbIfDataProvided(
        IList<ExpandoObject> seedItems,
        CosmosClient client,
        string containerId)
    {
        if (seedItems.Count > 0)
        {
            Container c = client.GetContainer(_databaseId, containerId);
            foreach (ExpandoObject item in seedItems)
            {
                await c.UpsertItemAsync(item);
            }
        }
    }
}
