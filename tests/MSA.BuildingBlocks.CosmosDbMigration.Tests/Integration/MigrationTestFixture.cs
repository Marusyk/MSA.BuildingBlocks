using System.Dynamic;
using Bogus;

namespace MSA.BuildingBlocks.CosmosDbMigration.Tests.Integration;

public sealed class MigrationTestFixture
{
    public List<ExpandoObject> InitialItems { get; }

    public MigrationTestFixture()
    {
        InitialItems = GenerateFakeItems(10);
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
