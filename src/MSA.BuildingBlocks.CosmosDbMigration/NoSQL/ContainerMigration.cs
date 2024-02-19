using Microsoft.Azure.Cosmos;
using MSA.BuildingBlocks.CosmosDbMigration.Abstractions;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;

namespace MSA.BuildingBlocks.CosmosDbMigration.NoSQL;
internal class ContainerMigration(CosmosClient cosmosClient, string databaseId, string containerId) : BaseContainerMigration(cosmosClient, databaseId, containerId)
{
    public override async Task<(IList<ExpandoObject>, double)> GetItems(string query = "SELECT * FROM c")
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
}
