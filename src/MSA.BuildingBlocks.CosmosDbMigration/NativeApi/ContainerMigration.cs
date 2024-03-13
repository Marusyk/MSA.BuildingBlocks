using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MSA.BuildingBlocks.CosmosDbMigration;

public class ContainerMigration(CosmosClient cosmosClient, string databaseId, string containerId, ILogger<ContainerMigration> logger)
    : BaseContainerMigration(cosmosClient, databaseId, containerId, logger)
{
    public override async Task<IList<ExpandoObject>> GetItems(string query = "SELECT * FROM c")
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

        _logger.LogInformation("{OperationName} with items count {Count} cost {Charge} RUs.", nameof(GetItems), items.Count, requestCharge);
        return items;
    }

    public override async Task SwitchToContainer(string containerId, string? databaseId = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(containerId);

        _container = _cosmosClient.GetContainer(databaseId ?? _container.Database.Id, containerId);
        _containerProperties = await _container.ReadContainerAsync().ConfigureAwait(false);

        _logger.LogInformation("Switching to container {ContainerId} and database {DatabaseId} is successful", containerId, databaseId);
    }

    public override async Task UpsertItems<T>(IList<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        double requestCharge = 0.0;
        foreach (T? item in items)
        {
            ItemResponse<T> upsertResponse = await _container.UpsertItemAsync(item).ConfigureAwait(false);
            requestCharge += upsertResponse.RequestCharge;
        }

        _logger.LogInformation("{OperationName} with items count {Count} cost {Charge} RUs.", nameof(UpsertItems), items.Count, requestCharge);
    }

    public override async Task AddPropertyToItems(IList<ExpandoObject> items, string propertyPath, string propertyName, object value)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(value);
        ArgumentException.ThrowIfNullOrEmpty(propertyName);
        ArgumentException.ThrowIfNullOrEmpty(propertyPath);

        double requestCharge = 0.0;
        foreach (ExpandoObject item in items)
        {
            object obj = DivingToNestedObject(item, propertyPath.Split('.'));
            if (!((IDictionary<string, object>)obj).TryAdd(propertyName, value))
            {
                throw new ArgumentException("Cannot add property because it exists. Use update than.");
            }

            ResponseMessage response = await ReplaceItem(item);
            requestCharge += response.Headers.RequestCharge;
        }

        _logger.LogInformation("{OperationName} with items count {Count} cost {Charge} RUs.", nameof(AddPropertyToItems), items.Count, requestCharge);
    }

    public override async Task AddPropertyToItems(IList<ExpandoObject> items, string propertyName, object value)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(value);
        ArgumentException.ThrowIfNullOrEmpty(propertyName);

        double requestCharge = 0.0;
        foreach (ExpandoObject item in items)
        {
            if (!item.TryAdd(propertyName, value))
            {
                throw new ArgumentException($"Cannot add property because it exists. Use update than.");
            }

            ResponseMessage response = await ReplaceItem(item);
            requestCharge += response.Headers.RequestCharge;
        }

        _logger.LogInformation("{OperationName} with items count {Count} cost {Charge} RUs.", nameof(AddPropertyToItems), items.Count, requestCharge);
    }

    public override async Task RemovePropertyFromItems(IList<ExpandoObject> items, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentException.ThrowIfNullOrEmpty(propertyName);

        double requestCharge = 0.0;
        foreach (ExpandoObject item in items)
        {
            if (!item.Remove(propertyName, out object? _))
            {
                continue;
            }

            ResponseMessage response = await ReplaceItem(item);
            requestCharge += response.Headers.RequestCharge;
        }

        _logger.LogInformation("{OperationName} with items count {Count} cost {Charge} RUs.", nameof(RemovePropertyFromItems), items.Count, requestCharge);
    }
    public override async Task RemovePropertyFromItems(IList<ExpandoObject> items, string propertyPath, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentException.ThrowIfNullOrEmpty(propertyName);

        double requestCharge = 0.0;
        foreach (ExpandoObject item in items)
        {
            object obj = DivingToNestedObject(item, propertyPath.Split('.'));
            if (!((IDictionary<string, object>)obj).Remove(propertyName, out object? _))
            {
                continue;
            }

            ResponseMessage response = await ReplaceItem(item);
            requestCharge += response.Headers.RequestCharge;
        }

        _logger.LogInformation("{OperationName} with items count {Count} cost {Charge} RUs.", nameof(RemovePropertyFromItems), items.Count, requestCharge);
    }

    private static object DivingToNestedObject(object obj, string[] pathParts)
    {
        if (pathParts.Length == 1)
        {
            return obj;
        }

        string nextPart = pathParts[0];
        if (obj is not IDictionary<string, object> nestedObject)
        {
            throw new ArgumentException($"Invalid property path: '{string.Join(".", pathParts)}'");
        }

        return DivingToNestedObject(nestedObject[nextPart], pathParts.Skip(1).ToArray());
    }

    private Task<ResponseMessage> ReplaceItem(ExpandoObject @object)
    {
        ArgumentNullException.ThrowIfNull(@object);

        string id = @object.FirstOrDefault(n => n.Key == "id").Value?.ToString()
            ?? throw new InvalidOperationException("Id property is not presented in the item.");

        string partitionKeyPath = _containerProperties.PartitionKeyPath[1..];
        string partitionKeyValue = @object.FirstOrDefault(n => n.Key == partitionKeyPath).Value?.ToString()
            ?? throw new InvalidOperationException($"Item with partition key {partitionKeyPath} is not presented.");

        return _container.ReplaceItemStreamAsync(GetItemStream(@object), id, new PartitionKey(partitionKeyValue));
    }

    private static MemoryStream GetItemStream(ExpandoObject item)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item));
        return new MemoryStream(bytes);
    }
}
