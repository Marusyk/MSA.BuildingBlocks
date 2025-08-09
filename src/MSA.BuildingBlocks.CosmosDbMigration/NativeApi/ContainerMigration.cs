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

/// <summary>
/// This class inherits from <see cref="BaseContainerMigration"/> and provides an implementation for Cosmos DB container migration operations.
/// </summary>
public class ContainerMigration : BaseContainerMigration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerMigration"/> class.
    /// </summary>
    /// <param name="cosmosClient">The Cosmos client instance.</param>
    /// <param name="databaseId">The ID of the existing database containing the target container.</param>
    /// <param name="containerId">The ID of the existing target container.</param>
    /// <param name="logger">Optional logger instance. If not provided, a default logger will be created.</param>
    /// <exception cref="ArgumentNullException">Thrown if cosmosClient is null.</exception>
    /// <exception cref="ArgumentException">Thrown if databaseId or containerId is null or empty.</exception>
    public ContainerMigration(
        CosmosClient cosmosClient,
        string databaseId,
        string containerId,
        ILogger<ContainerMigration>? logger = default)
        : base(cosmosClient, databaseId, containerId, logger)
    { }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public override ValueTask SwitchToContainer(string containerId, string? databaseId = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(containerId);
        databaseId ??= _container.Database.Id;

        _container = _cosmosClient.GetContainer(databaseId, containerId);

        _logger.LogInformation("Switching to container {ContainerId} and database {DatabaseId} is successful", containerId, databaseId);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public override async Task RemoveItemsByQuery(string query)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        IList<ExpandoObject> items = await GetItems(query);
        double requestCharge = 0.0;

        ContainerProperties containerProperties = await _container.ReadContainerAsync().ConfigureAwait(false);
        foreach (ExpandoObject item in items)
        {
            string itemId = item.First(i => i.Key == "id").Value!.ToString()!;
            object itemPartitionKeyValue = item.First(i => i.Key == containerProperties.PartitionKeyPath[1..]).Value!;

            ResponseMessage response = await _container.DeleteItemStreamAsync(itemId, GetPartitionKey(itemPartitionKeyValue), new ItemRequestOptions { EnableContentResponseOnWrite = false });
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Item with id {Id} and partition key {PartitionKey} wasn't replaced due to Error: {Message}",
                    itemId, itemPartitionKeyValue, response.ErrorMessage);

                requestCharge += response.Headers.RequestCharge;
                continue;
            }

            _logger.LogInformation("Item with id {Id} and partition key {Key} is deleted.", itemId, itemPartitionKeyValue);
            requestCharge += response.Headers.RequestCharge;
        }

        _logger.LogInformation("{OperationName} with items count {Count} cost {Charge} RUs.", nameof(RemoveItemsByQuery), items.Count, requestCharge);
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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
                throw new ArgumentException($"Cannot add property because it exists. Use update.", nameof(propertyName));
            }

            ResponseMessage response = await ReplaceItem(item);
            requestCharge += response.Headers.RequestCharge;
        }

        _logger.LogInformation("{OperationName} with items count {Count} cost {Charge} RUs.", nameof(AddPropertyToItems), items.Count, requestCharge);
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public override async Task RemovePropertyFromItems(IList<ExpandoObject> items, string propertyPath, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentException.ThrowIfNullOrEmpty(propertyPath);
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
        if (pathParts.Length == 0)
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

    private async Task<ResponseMessage> ReplaceItem(ExpandoObject @object)
    {
        ArgumentNullException.ThrowIfNull(@object);

        string id = @object.FirstOrDefault(n => n.Key == "id").Value?.ToString()
            ?? throw new InvalidOperationException("Id property is not presented in the item.");

        ContainerProperties containerProperties = await _container.ReadContainerAsync().ConfigureAwait(false);
        string partitionKeyPath = containerProperties.PartitionKeyPath[1..];
        object partitionKeyValue = @object.FirstOrDefault(n => n.Key == partitionKeyPath).Value
            ?? throw new InvalidOperationException($"Item with partition key {partitionKeyPath} is not presented.");

        ResponseMessage response = await _container.ReplaceItemStreamAsync(GetItemStream(@object), id, GetPartitionKey(partitionKeyValue));
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Item with id {Id} and partition key {PartitionKey} wasn't replaced due to Error: {Message}",
                id, partitionKeyValue, response.ErrorMessage);
        }
        return response;
    }

    private static PartitionKey GetPartitionKey(object partitionKeyObject) =>
         partitionKeyObject switch
         {
             string value => new PartitionKey(value),
             long value => new PartitionKey(value),
             double value => new PartitionKey(value),
             byte value => new PartitionKey(value),
             _ => throw new ArgumentException($"Unsupported partition key type {partitionKeyObject.GetType()}.")
         };

    private static MemoryStream GetItemStream(ExpandoObject item)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item));
        return new MemoryStream(bytes);
    }
}
