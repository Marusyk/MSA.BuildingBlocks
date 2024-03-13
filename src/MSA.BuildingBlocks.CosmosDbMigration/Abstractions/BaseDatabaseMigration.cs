using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Threading.Tasks;

namespace MSA.BuildingBlocks.CosmosDbMigration;

public abstract class BaseDatabaseMigration
{
    protected CosmosClient _cosmosClient;
    protected Container _container;
    protected ContainerProperties _containerProperties;
    protected ILogger<BaseDatabaseMigration> _logger;

    protected BaseDatabaseMigration(CosmosClient cosmosClient, string databaseId, string containerId, ILogger<BaseDatabaseMigration> logger)
    {
        ArgumentNullException.ThrowIfNull(cosmosClient);
        ArgumentException.ThrowIfNullOrEmpty(databaseId);
        ArgumentException.ThrowIfNullOrEmpty(containerId);

        _cosmosClient = cosmosClient;
        _container = cosmosClient.GetContainer(databaseId, containerId);
        _containerProperties = _container.ReadContainerAsync().GetAwaiter().GetResult();
        _logger = logger;
    }

    public abstract Task CreateContainer(string containerId, string partitionKey);

    public abstract Task CloneContainer(string containerId, string partitionKey);

    public abstract Task DeleteContainer();

    public abstract Task RecreateContainerWithNewPartitionKey(string partitionKey);

    public abstract Task AddIndexingPolicy(
        Collection<IncludedPath>? includedPaths = null,
        Collection<ExcludedPath>? excludedPaths = null,
        Collection<Collection<CompositePath>>? compositePaths = null);

    public abstract Task ReplaceIndexingPolicy(
        Collection<IncludedPath>? includedPaths = null,
        Collection<ExcludedPath>? excludedPaths = null,
        Collection<Collection<CompositePath>>? compositePaths = null);

    public abstract Task SwitchToContainer(string containerId, string? databaseId = null);

    protected abstract Task<(IList<ExpandoObject>, double)> GetItems(string query = "SELECT * FROM c");
}
