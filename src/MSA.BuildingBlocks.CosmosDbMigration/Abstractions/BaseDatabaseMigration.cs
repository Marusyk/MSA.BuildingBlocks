using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Threading.Tasks;

namespace MSA.BuildingBlocks.CosmosDbMigration;

/// <summary>
/// This abstract class provides base operations for Cosmos Db migrations.
/// </summary>
public abstract class BaseDatabaseMigration
{
    protected CosmosClient _cosmosClient;
    protected Container _container;
    protected ContainerProperties _containerProperties;
    protected ILogger<BaseDatabaseMigration> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseDatabaseMigration"/> class.
    /// </summary>
    /// <param name="cosmosClient">The Cosmos client instance.</param>
    /// <param name="databaseId">The ID of the existing database containing the target container.</param>
    /// <param name="containerId">The ID of the existing target container.</param>
    /// <param name="logger">Optional logger instance. If not provided, a default logger will be created.</param>
    /// <exception cref="ArgumentNullException">Thrown if cosmosClient is null.</exception>
    /// <exception cref="ArgumentException">Thrown if databaseId or containerId is null or empty.</exception>
    protected BaseDatabaseMigration(
        CosmosClient cosmosClient,
        string databaseId,
        string containerId,
        ILogger<BaseDatabaseMigration>? logger = default)
    {
        ArgumentNullException.ThrowIfNull(cosmosClient);
        ArgumentException.ThrowIfNullOrEmpty(databaseId);
        ArgumentException.ThrowIfNullOrEmpty(containerId);

        _cosmosClient = cosmosClient;
        _container = cosmosClient.GetContainer(databaseId, containerId);
        _containerProperties = _container.ReadContainerAsync().GetAwaiter().GetResult();
        _logger = logger ?? new LoggerFactory().CreateLogger<BaseDatabaseMigration>();
    }

    /// <summary>
    /// Creates a new container as a clone of the existing target container, including its data and indexing policy.
    /// </summary>
    /// <param name="containerId">The ID for the new container.</param>
    /// <param name="partitionKey">The partition key path for the new container.</param>
    /// <returns>An asynchronous task that completes the container cloning.</returns>
    /// <exception cref="ArgumentException">Thrown if containerId or partitionKey is null or empty.</exception>
    public abstract Task CloneContainer(string containerId, string partitionKey);

    /// <summary>
    /// Creates a new container within the current database with the specified ID and partition key path.
    /// </summary>
    /// <param name="containerId">The ID for the new container. Must not be null or empty.</param>
    /// <param name="partitionKey">The partition key path for the new container.</param>
    /// <returns>An asynchronous task that completes the container creation.</returns>
    /// <exception cref="ArgumentException">Thrown if containerId or partitionKey is null or empty.</exception>
    public abstract Task CreateContainer(string containerId, string partitionKey);

    /// <summary>
    /// Deletes the target container.
    /// </summary>
    /// <returns>An asynchronous task that completes the container deletion.</returns>
    public abstract Task DeleteContainer();

    /// <summary>
    /// Recreates the target container with a new specified partition key path. Data and indexing policy are not preserved.
    /// </summary>
    /// <param name="partitionKey">The new partition key path for the target container.</param>
    /// <returns>An asynchronous task that completes the container recreation.</returns>
    public abstract Task RecreateContainerWithNewPartitionKey(string partitionKey);

    /// <summary>
    /// Updates the indexing policy for the target container.
    /// </summary>
    /// <param name="includedPaths">Optional collection of included paths for indexing.</param>
    /// <param name="excludedPaths">Optional collection of excluded paths for indexing.</param>
    /// <param name="compositePaths">Optional collection of composite paths for indexing.</param>
    /// <returns>An asynchronous task that completes the indexing policy update.</returns>
    public abstract Task AddIndexingPolicy(
        Collection<IncludedPath>? includedPaths = null,
        Collection<ExcludedPath>? excludedPaths = null,
        Collection<Collection<CompositePath>>? compositePaths = null);

    /// <summary>
    /// Replaces the existing indexing policy for the target container.
    /// </summary>
    /// <param name="includedPaths">Optional collection of included paths for indexing.</param>
    /// <param name="excludedPaths">Optional collection of excluded paths for indexing.</param>
    /// <param name="compositePaths">Optional collection of composite paths for indexing.</param>
    /// <returns>An asynchronous task that completes the indexing policy replacement.</returns>
    public abstract Task ReplaceIndexingPolicy(
        Collection<IncludedPath>? includedPaths = null,
        Collection<ExcludedPath>? excludedPaths = null,
        Collection<Collection<CompositePath>>? compositePaths = null);

    /// <summary>
    /// Switches the target container to a different container within the Cosmos DB.
    /// </summary>
    /// <param name="containerId">The ID of the new target container.</param>
    /// <param name="databaseId">Optional ID of the database containing the new target container. If not provided, uses the current database.</param>
    /// <returns>An asynchronous task that completes the container switch.</returns>
    /// <exception cref="ArgumentNullException">Thrown if containerId is null or empty.</exception>
    public abstract Task SwitchToContainer(string containerId, string? databaseId = null);

    protected abstract Task<(IList<ExpandoObject>, double)> GetItems(string query = "SELECT * FROM c");
}
