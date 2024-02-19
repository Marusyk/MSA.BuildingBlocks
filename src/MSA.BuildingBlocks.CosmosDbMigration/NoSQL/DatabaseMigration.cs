using Azure;
using Microsoft.Azure.Cosmos;
using MSA.BuildingBlocks.CosmosDbMigration.Abstractions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSA.BuildingBlocks.CosmosDbMigration.NoSQL;

public class DatabaseMigration(CosmosClient cosmosClient, string databaseId, string containerId)
    : BaseDatabaseMigration(cosmosClient, databaseId, containerId)
{
    public override async Task CloneContainer(string containerId, string partitionKey)
    {
        (IList<ExpandoObject> items, double requestCharge) = await GetItems().ConfigureAwait(false);

        double createContainerAndUploadItemsCharge = await CreateContainerAndUploadItems(containerId, partitionKey, items).ConfigureAwait(false);
        requestCharge += createContainerAndUploadItemsCharge;

        Console.WriteLine($"{nameof(CloneContainer)} with items count {items.Count} operation cost {requestCharge} RUs.");
    }

    public override async Task DeleteContainer()
    {
        ContainerResponse response = await _container.DeleteContainerAsync().ConfigureAwait(false);
        Console.WriteLine($"{nameof(DeleteContainer)} operation cost {response.RequestCharge} RUs.");
    }

    public override async Task CreateContainer(string containerId, string partitionKey)
    {
        ContainerResponse response = await _container.Database.CreateContainerIfNotExistsAsync(containerId, $"/{partitionKey}").ConfigureAwait(false);
        Console.WriteLine($"{nameof(CreateContainer)} operation cost {response.RequestCharge} RUs.");
    }

    public override async Task RecreateContainerWithNewPartitionKey(string partitionKey)
    {
        ArgumentException.ThrowIfNullOrEmpty(partitionKey, nameof(partitionKey));

        (IList<ExpandoObject> items, double requestCharge) = await GetItems().ConfigureAwait(false);

        ContainerResponse deleteContainerResponse = await _container.DeleteContainerAsync().ConfigureAwait(false);
        requestCharge += deleteContainerResponse.RequestCharge;

        double createContainerAndUploadItemsCharge = await CreateContainerAndUploadItems(_container.Id, partitionKey, items).ConfigureAwait(false);
        requestCharge += createContainerAndUploadItemsCharge;

        Console.WriteLine($"{nameof(RecreateContainerWithNewPartitionKey)} operation with items count {items.Count} cost {requestCharge} RUs.");
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
        Console.WriteLine($"{nameof(ReplaceIndexingPolicy)} operation cost {response.RequestCharge} RUs.");
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

        static MemoryStream GetItemStream(ExpandoObject item)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item));
            return new MemoryStream(bytes);
        }
    }

    public override async Task SwitchToContainer(string containerId, string? databaseId = null)
    {
        _container = _client.GetContainer(databaseId ?? _container.Database.Id, containerId);
        _containerProperties = await _container.ReadContainerAsync().ConfigureAwait(false);
    }

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
            Console.WriteLine($"{nameof(AddIndexingPolicy)} operation cost {response.RequestCharge} RUs.");
            return;
        }

        Console.WriteLine($"{nameof(AddIndexingPolicy)} operation does not apply because nothing new added.");
    }
}
