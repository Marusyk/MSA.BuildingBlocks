using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;

namespace MSA.BuildingBlocks.CosmosDbMigration;

/// <summary>
/// This abstract class provides base operations for Cosmos Db container migrations.
/// </summary>
public abstract class BaseContainerMigration
{
    protected CosmosClient _cosmosClient;
    protected Container _container;
    protected ContainerProperties _containerProperties;
    protected ILogger<BaseContainerMigration> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseContainerMigration"/> derived classes.
    /// </summary>
    /// <param name="cosmosClient">The Cosmos client instance.</param>
    /// <param name="databaseId">The ID of the existing database containing the target container.</param>
    /// <param name="containerId">The ID of the existing target container.</param>
    /// <param name="logger">Optional logger instance. If not provided, a default logger will be created.</param>
    /// <exception cref="ArgumentNullException">Thrown if cosmosClient is null.</exception>
    /// <exception cref="ArgumentException">Thrown if databaseId or containerId is null or empty.</exception>
    protected BaseContainerMigration(
        CosmosClient cosmosClient,
        string databaseId,
        string containerId,
        ILogger<BaseContainerMigration>? logger = default)
    {
        ArgumentNullException.ThrowIfNull(cosmosClient);
        ArgumentException.ThrowIfNullOrEmpty(databaseId);
        ArgumentException.ThrowIfNullOrEmpty(containerId);

        _cosmosClient = cosmosClient;
        _container = cosmosClient.GetContainer(databaseId, containerId);
        _containerProperties = _container.ReadContainerAsync().GetAwaiter().GetResult();
        _logger = logger ?? new LoggerFactory().CreateLogger<BaseContainerMigration>();
    }

    /// <summary>
    /// Retrieves a list of items from the container using a provided query.
    /// </summary>
    /// <param name="query">Optional SQL query to execute. Defaults to "SELECT * FROM c".</param>
    /// <returns>An asynchronous task that returns a list of ExpandoObject instances representing the retrieved items.</returns>
    public abstract Task<IList<ExpandoObject>> GetItems(string query = "SELECT * FROM c");

    /// <summary>
    /// Switches the target container to a different container within the Cosmos DB.
    /// </summary>
    /// <param name="containerId">The ID of the new target container.</param>
    /// <param name="databaseId">Optional ID of the database containing the new target container. If not provided, uses the current database.</param>
    /// <returns>An asynchronous task that completes the container switch.</returns>
    public abstract Task SwitchToContainer(string containerId, string? databaseId = null);

    /// <summary>
    /// Upserts a list of items into the target container.
    /// </summary>
    /// <typeparam name="T">The type of the items to be upserted.</typeparam>
    /// <param name="items">The list of items to upsert.</param>
    /// <returns>An asynchronous task that completes the upsert operation.</returns>
    public abstract Task UpsertItems<T>(IList<T> items);

    /// <summary>
    /// Removes items from the container based on a provided query.
    /// </summary>
    /// <param name="query">The SQL query to identify items for removal.</param>
    /// <returns>An asynchronous task that completes the removal operation.</returns>
    public abstract Task RemoveItemsByQuery(string query);

    /// <summary>
    /// Adds a new property to a list of ExpandoObject instances.
    /// </summary>
    /// <param name="items">The list of items to modify.</param>
    /// <param name="propertyName">The name of the property to add.</param>
    /// <param name="value">The value to assign to the new property.</param>
    /// <returns>An asynchronous task that completes the property addition.</returns>
    public abstract Task AddPropertyToItems(IList<ExpandoObject> items, string propertyName, object value);

    /// <summary>
    /// Adds a new property to a list of ExpandoObject instances.
    /// </summary>
    /// <param name="items">The list of items to modify.</param>
    /// <param name="propertyPath">Optional path to a nested property within the object. If omitted, adds to the root level.</param>
    /// <param name="propertyName">The name of the property to add.</param>
    /// <param name="value">The value to assign to the new property.</param>
    /// <returns>An asynchronous task that completes the property addition.</returns>
    public abstract Task AddPropertyToItems(IList<ExpandoObject> items, string propertyPath, string propertyName, object value);

    /// <summary>
    /// Removes a property from a list of ExpandoObject instances.
    /// </summary>
    /// <param name="items">The list of items to modify.</param>
    /// <param name="propertyName">The name of the property to remove.</param>
    /// <returns>An asynchronous task that completes the property removal.</returns>
    public abstract Task RemovePropertyFromItems(IList<ExpandoObject> items, string propertyName);

    /// <summary>
    /// Removes a property from a list of ExpandoObject instances, targeting a specific nested location.
    /// </summary>
    /// <param name="items">The list of items to modify.</param>
    /// <param name="propertyPath">The path to the nested property to remove.</param>
    /// <param name="propertyName">The name of the property to remove within the specified path.</param>
    /// <returns>An asynchronous task that completes the property removal.</returns>
    public abstract Task RemovePropertyFromItems(IList<ExpandoObject> items, string propertyPath, string propertyName);
}
