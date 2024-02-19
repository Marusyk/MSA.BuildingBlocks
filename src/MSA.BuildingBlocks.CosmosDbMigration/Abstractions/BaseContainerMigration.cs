using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSA.BuildingBlocks.CosmosDbMigration.Abstractions;

public abstract class BaseContainerMigration
{

    protected Container _container;
    protected ContainerProperties _properties;

    protected BaseContainerMigration(CosmosClient cosmosClient, string databaseId, string containerId)
    {
        ArgumentNullException.ThrowIfNull(cosmosClient, nameof(cosmosClient));
        ArgumentException.ThrowIfNullOrEmpty(databaseId, nameof(databaseId));
        ArgumentException.ThrowIfNullOrEmpty(containerId, nameof(containerId));

        _container = cosmosClient.GetContainer(databaseId, containerId);
        _properties = _container.ReadContainerAsync().GetAwaiter().GetResult();
    }

    public abstract Task<(IList<ExpandoObject>, double)> GetItems(string query = "SELECT * FROM c");
}
