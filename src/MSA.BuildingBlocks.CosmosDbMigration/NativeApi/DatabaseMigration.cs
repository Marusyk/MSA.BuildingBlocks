using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSA.BuildingBlocks.CosmosDbMigration;

/// <summary>
/// This class inherits from <see cref="BaseDatabaseMigration"/> and provides an implementation for Cosmos DB migration operations.
/// </summary>
/// <param name="cosmosClient">The Cosmos client instance.</param>
/// <param name="databaseId">The ID of the existing database containing the target container.</param>
/// <param name="containerId">The ID of the existing target container.</param>
/// <param name="logger">Optional logger instance. If not provided, a default logger will be created.</param>
/// <exception cref="ArgumentNullException">Thrown if cosmosClient is null.</exception>
/// <exception cref="ArgumentException">Thrown if databaseId or containerId is null or empty.</exception>
public class DatabaseMigration(
    CosmosClient cosmosClient,
    string databaseId,
    string containerId,
    ILogger<DatabaseMigration>? logger = default)
    : BaseDatabaseMigration(cosmosClient, databaseId, containerId, logger)
{
    /// <inheritdoc/>
    public override async Task CloneContainer(string containerId, string partitionKey)
    {
        ArgumentException.ThrowIfNullOrEmpty(containerId);
        ArgumentException.ThrowIfNullOrEmpty(partitionKey);

        (IList<ExpandoObject> items, double requestCharge) = await GetItems().ConfigureAwait(false);

        double createContainerAndUploadItemsCharge = await CreateContainerAndUploadItems(containerId, partitionKey, items).ConfigureAwait(false);
        requestCharge += createContainerAndUploadItemsCharge;

        _logger.LogInformation("{OperationName} with items count {Count} operation cost {Charge} RUs.", nameof(CloneContainer), items.Count, requestCharge);
    }

    /// <inheritdoc/>
    public override async Task DeleteContainer()
    {
        ContainerResponse response = await _container.DeleteContainerAsync().ConfigureAwait(false);

        _logger.LogInformation("{OperationName} operation cost {Charge} RUs.", nameof(DeleteContainer), response.RequestCharge);
    }

    /// <inheritdoc/>
    public override async Task CreateContainer(string containerId, string partitionKey)
    {
        ArgumentException.ThrowIfNullOrEmpty(containerId);
        ArgumentException.ThrowIfNullOrEmpty(partitionKey);

        ContainerResponse response = await _container.Database.CreateContainerIfNotExistsAsync(containerId, $"/{partitionKey}").ConfigureAwait(false);

        _logger.LogInformation("{OperationName} operation cost {Charge} RUs.", nameof(CreateContainer), response.RequestCharge);
    }

    /// <inheritdoc/>
    public override async Task RecreateContainerWithNewPartitionKey(string partitionKey)
    {
        ArgumentException.ThrowIfNullOrEmpty(partitionKey);

        (IList<ExpandoObject> items, double requestCharge) = await GetItems().ConfigureAwait(false);

        ContainerResponse deleteContainerResponse = await _container.DeleteContainerAsync().ConfigureAwait(false);
        requestCharge += deleteContainerResponse.RequestCharge;

        double createContainerAndUploadItemsCharge = await CreateContainerAndUploadItems(_container.Id, partitionKey, items).ConfigureAwait(false);
        requestCharge += createContainerAndUploadItemsCharge;

        _logger.LogInformation("{OperationName} with items count {Count} operation cost {Charge} RUs.", nameof(RecreateContainerWithNewPartitionKey), items.Count, requestCharge);
    }

    /// <inheritdoc/>
    public override async Task ReplaceIndexingPolicy(
        Collection<IncludedPath>? includedPaths = null,
        Collection<ExcludedPath>? excludedPaths = null,
        Collection<Collection<CompositePath>>? compositePaths = null)
    {
        ContainerProperties containerProperties = new(_containerProperties.Id, _containerProperties.PartitionKeyPath);

        if (includedPaths is not null)
        {
            containerProperties.IndexingPolicy.IncludedPaths.Clear();

            foreach (IncludedPath includedPath in includedPaths)
            {
                containerProperties.IndexingPolicy.IncludedPaths.Add(includedPath);
            }
        }

        if (excludedPaths is not null)
        {
            containerProperties.IndexingPolicy.ExcludedPaths.Clear();

            foreach (ExcludedPath excludedPath in excludedPaths)
            {
                containerProperties.IndexingPolicy.ExcludedPaths.Add(excludedPath);
            }
        }

        if (compositePaths is not null)
        {
            containerProperties.IndexingPolicy.CompositeIndexes.Clear();

            foreach (Collection<CompositePath> compositePath in compositePaths)
            {
                containerProperties.IndexingPolicy.CompositeIndexes.Add(compositePath);
            }
        }

        ContainerResponse response = await _container.ReplaceContainerAsync(containerProperties).ConfigureAwait(false);

        _logger.LogInformation("{OperationName} operation cost {Charge} RUs.", nameof(ReplaceIndexingPolicy), response.RequestCharge);
    }

    /// <inheritdoc/>
    public override async Task SwitchToContainer(string containerId, string? databaseId = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(containerId);
        databaseId ??= _container.Database.Id;

        _container = _cosmosClient.GetContainer(databaseId, containerId);
        _containerProperties = await _container.ReadContainerAsync().ConfigureAwait(false);

        _logger.LogInformation("Switching to container {ContainerId} and database {DatabaseId} is successful", containerId, databaseId);
    }

    /// <inheritdoc/>
    public override async Task AddIndexingPolicy(
        Collection<IncludedPath>? includedPaths = null,
        Collection<ExcludedPath>? excludedPaths = null,
        Collection<Collection<CompositePath>>? compositePaths = null)
    {
        bool indexesChanged = false;

        if (includedPaths is not null)
        {
            foreach (IncludedPath includedPath in includedPaths)
            {
                if (_containerProperties.IndexingPolicy.IncludedPaths.FirstOrDefault(i => i.Path == includedPath.Path) is not null)
                {
                    continue;
                }

                indexesChanged = true;
                _containerProperties.IndexingPolicy.IncludedPaths.Add(includedPath);
            }
        }

        if (excludedPaths is not null)
        {
            foreach (ExcludedPath excludedPath in excludedPaths)
            {
                if (_containerProperties.IndexingPolicy.ExcludedPaths.FirstOrDefault(i => i.Path == excludedPath.Path) is not null)
                {
                    continue;
                }

                indexesChanged = true;
                _containerProperties.IndexingPolicy.ExcludedPaths.Add(excludedPath);
            }
        }

        if (compositePaths is not null)
        {
            List<Collection<CompositePath>> existingCompositeIndexes = [.. _containerProperties.IndexingPolicy.CompositeIndexes];
            foreach (Collection<CompositePath> compositePath in compositePaths)
            {
                bool indexesExist = false;
                foreach (Collection<CompositePath> existingCompositePath in existingCompositeIndexes)
                {
                    if (compositePath.All(path => existingCompositePath.Any(existingPath =>
                        existingPath.Path == path.Path && existingPath.Order == path.Order)))
                    {
                        indexesExist = true;
                        break;
                    }
                }

                if (!indexesExist)
                {
                    indexesChanged = true;
                    _containerProperties.IndexingPolicy.CompositeIndexes.Add(compositePath);
                }
            }

            if (_containerProperties.IndexingPolicy.CompositeIndexes.Count is 0)
            {
                indexesChanged = true;

                foreach (Collection<CompositePath> compositePath in compositePaths)
                {
                    _containerProperties.IndexingPolicy.CompositeIndexes.Add(compositePath);
                }
            }
        }

        if (indexesChanged)
        {
            ContainerResponse response = await _container.ReplaceContainerAsync(_containerProperties).ConfigureAwait(false);

            _logger.LogInformation("{OperationName} operation cost {Charge} RUs.", nameof(AddIndexingPolicy), response.RequestCharge);
            return;
        }

        _logger.LogInformation("{OperationName} operation does not apply because nothing new added.", nameof(AddIndexingPolicy));
    }

    private async Task<double> CreateContainerAndUploadItems(string containerId, string partitionKey, IEnumerable<ExpandoObject> items)
    {
        ContainerResponse createContainerResponse = await _container.Database.CreateContainerIfNotExistsAsync(containerId, $"/{partitionKey}").ConfigureAwait(false);
        double requestCharge = createContainerResponse.RequestCharge;

        foreach (ExpandoObject item in items)
        {
            PartitionKey key = new(item.FirstOrDefault(x => x.Key == partitionKey).Value?.ToString());
            ResponseMessage createItemResponse = await createContainerResponse.Container.CreateItemStreamAsync(GetItemStream(item), key).ConfigureAwait(false);
            requestCharge += createItemResponse.Headers.RequestCharge;
        }

        _container = createContainerResponse.Container;
        _container = await createContainerResponse.Container.ReadContainerAsync().ConfigureAwait(false);

        return requestCharge;
    }

    protected override async Task<(IList<ExpandoObject>, double)> GetItems(string query = "SELECT * FROM c")
    {
        double requestCharge = 0.0;
        List<ExpandoObject> items = [];

        using FeedIterator<ExpandoObject> feedIterator = _container.GetItemQueryIterator<ExpandoObject>(new QueryDefinition(query));
        while (feedIterator.HasMoreResults)
        {
            FeedResponse<ExpandoObject> feedItems = await feedIterator.ReadNextAsync().ConfigureAwait(false);
            requestCharge += feedItems.RequestCharge;

            items.AddRange(feedItems);
        }

        return (items, requestCharge);
    }

    private static MemoryStream GetItemStream(ExpandoObject item)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item));
        return new MemoryStream(bytes);
    }
}
