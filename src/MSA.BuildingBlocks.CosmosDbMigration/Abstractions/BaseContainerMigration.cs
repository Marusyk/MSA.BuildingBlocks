using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;

namespace MSA.BuildingBlocks.CosmosDbMigration;

public abstract class BaseContainerMigration
{
    protected CosmosClient _cosmosClient;
    protected Container _container;
    protected ContainerProperties _containerProperties;
    protected ILogger<BaseContainerMigration> _logger;

    protected BaseContainerMigration(CosmosClient cosmosClient, string databaseId, string containerId, ILogger<BaseContainerMigration> logger)
    {
        ArgumentNullException.ThrowIfNull(cosmosClient);
        ArgumentException.ThrowIfNullOrEmpty(databaseId);
        ArgumentException.ThrowIfNullOrEmpty(containerId);

        _cosmosClient = cosmosClient;
        _container = cosmosClient.GetContainer(databaseId, containerId);
        _containerProperties = _container.ReadContainerAsync().GetAwaiter().GetResult();
        _logger = logger;
    }

    public abstract Task<IList<ExpandoObject>> GetItems(string query = "SELECT * FROM c");

    public abstract Task SwitchToContainer(string containerId, string? databaseId = null);

    public abstract Task UpsertItems<T>(IList<T> items);

    public abstract Task RemoveItemsByQuery(string query);

    public abstract Task AddPropertyToItems(IList<ExpandoObject> items, string propertyPath, string propertyName, object value);

    public abstract Task AddPropertyToItems(IList<ExpandoObject> items, string propertyName, object value);

    public abstract Task RemovePropertyFromItems(IList<ExpandoObject> items, string propertyName);

    public abstract Task RemovePropertyFromItems(IList<ExpandoObject> items, string propertyPath, string propertyName);
}
