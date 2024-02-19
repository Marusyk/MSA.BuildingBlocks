using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Threading.Tasks;

namespace MSA.BuildingBlocks.CosmosDbMigration.Abstractions;

public abstract class BaseDatabaseMigration
{
    protected CosmosClient _client;
    protected Container _container;
    protected ContainerProperties _containerProperties;

    protected BaseDatabaseMigration(CosmosClient cosmosClient, string databaseId, string containerId)
    {
        ArgumentNullException.ThrowIfNull(cosmosClient ,nameof(cosmosClient));
        ArgumentException.ThrowIfNullOrEmpty(databaseId, nameof(databaseId));
        ArgumentException.ThrowIfNullOrEmpty(containerId, nameof(containerId));

        _client = cosmosClient;
        _container = cosmosClient.GetContainer(databaseId, containerId);
        _containerProperties = _container.ReadContainerAsync().GetAwaiter().GetResult();
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

    protected abstract Task<(IList<ExpandoObject>, double)> GetItems(string query = "SELECT * FROM c");

    public abstract Task SwitchToContainer(string containerId, string? databaseId = null);
}
