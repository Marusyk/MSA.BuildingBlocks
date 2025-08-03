using System.Dynamic;
using Bogus;
using Randomizer = Bogus.Randomizer;

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
        Randomizer.Seed = new Random(1234);

        var faker = new Faker();

        return Enumerable
            .Range(1, count)
            .Select(i =>
            {
                dynamic item = new ExpandoObject();
                var dict = (IDictionary<string, object>)item;

                dict["id"] = Guid.NewGuid().ToString();
                dict["SomeField"] = "SomeField";
                dict["MyProperty"] = i;
                dict["MyProperty2"] = faker.Random.Int(1, 100);
                dict["MyProperty3"] = faker.Random.Int(1, 100);
                dict["CountryCode"] = faker.Address.CountryCode();
                dict["PostalCode"] = faker.Address.ZipCode();
                dict["InnerClass"] = new ExpandoObject();

                return (ExpandoObject)item;
            })
            .ToList();
    }
}
