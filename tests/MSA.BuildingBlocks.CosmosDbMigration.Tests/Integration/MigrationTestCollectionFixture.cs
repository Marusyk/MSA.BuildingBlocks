using System.Dynamic;
using Bogus;
using Microsoft.Azure.Cosmos;
using Xunit;

namespace MSA.BuildingBlocks.CosmosDbMigration.Tests.Integration;

[CollectionDefinition("Migration collection")]
public class MigrationCollection : ICollectionFixture<MigrationTestCollectionFixture>
{
}

public sealed class MigrationTestCollectionFixture : IAsyncLifetime
{
    public List<ExpandoObject> InitialItems { get; private set; }
    public CosmosClient CosmosClient { get; private set; }

    public ValueTask InitializeAsync()
    {
        CosmosClient = new CosmosClient("AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==");
        InitialItems = GenerateFakeItems(10);
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        CosmosClient.Dispose();
        return ValueTask.CompletedTask;
    }

    private static List<ExpandoObject> GenerateFakeItems(int count)
    {
        Faker faker = new();

        return Enumerable
            .Range(1, count)
            .Select(i =>
            {
                dynamic item = new ExpandoObject();

                item.id = Guid.NewGuid().ToString();
                item.SomeField = "SomeField";
                item.MyProperty = i;
                item.MyProperty2 = faker.Random.Int(1, 100);
                item.MyProperty3 = faker.Random.Int(1, 100);
                item.CountryCode = faker.Address.CountryCode();
                item.PostalCode = faker.Address.ZipCode();
                item.InnerClass = new ExpandoObject();
                 
                return (ExpandoObject)item;
            })
            .ToList();
    }
}
