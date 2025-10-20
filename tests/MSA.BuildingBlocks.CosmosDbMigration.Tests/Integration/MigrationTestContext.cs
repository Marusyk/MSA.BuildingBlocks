using System.Dynamic;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace MSA.BuildingBlocks.CosmosDbMigration.Tests.Integration;

public sealed class MigrationTestContext<TMigration> : IAsyncDisposable
{
    private readonly string _databaseId;
    private readonly CosmosClient _client;

    public TMigration Migration { get; }
    public Database Database { get; }
    public Container Container { get; }
    public string PartitionKey { get; }

    private MigrationTestContext(
        CosmosClient client,
        string databaseId,
        Database database,
        Container container,
        string partitionKey,
        TMigration migration)
    {
        _client = client;
        _databaseId = databaseId;
        Database = database;
        Container = container;
        PartitionKey = partitionKey;
        Migration = migration;
    }

    public static async Task<MigrationTestContext<TMigration>> CreateAsync(
        Func<CosmosClient, string, string, ILogger<TMigration>, TMigration> migrationFactory,
        CosmosClient client,
        IList<ExpandoObject> seedItems)
    {
        string databaseId = $"TestDb_{Guid.NewGuid()}";
        Database database = await client.CreateDatabaseAsync(databaseId);

        string containerId = $"TestContainer_{Guid.NewGuid()}";
        string partitionKey = "CountryCode";
        ContainerProperties containerProps = new(containerId, $"/{partitionKey}");
        Container container = await database.CreateContainerAsync(containerProps);

        ILogger<TMigration> logger = new LoggerFactory().CreateLogger<TMigration>();
        TMigration migration = migrationFactory(client, databaseId, containerId, logger);

        await SeedDbIfDataProvided(seedItems, client, containerId, databaseId);

        return new MigrationTestContext<TMigration>(
            client,
            databaseId,
            database,
            container,
            partitionKey,
            migration);
    }

    public async ValueTask DisposeAsync()
    {
        await _client.GetDatabase(_databaseId).DeleteAsync();
    }

    private static async Task SeedDbIfDataProvided(
        IList<ExpandoObject> seedItems,
        CosmosClient client,
        string containerId,
        string databaseId)
    {
        if (seedItems.Count > 0)
        {
            List<Task> upsertTasks = [];
            Container container = client.GetContainer(databaseId, containerId);

            foreach (ExpandoObject item in seedItems)
            {
                upsertTasks.Add(container.UpsertItemAsync(item));
            }

            await Task.WhenAll(upsertTasks);
        }
    }

}
