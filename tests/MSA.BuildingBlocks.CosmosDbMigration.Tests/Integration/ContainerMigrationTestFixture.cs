using System.Dynamic;
using Bogus;
using Randomizer = Bogus.Randomizer;

namespace MSA.BuildingBlocks.CosmosDbMigration.Tests.Integration;

public sealed class ContainerMigrationTestFixture
{
    public List<ExpandoObject> InitialItems { get; }

    public ContainerMigrationTestFixture()
    {
        InitialItems = GenerateFakeItems(10);
    }

    private static List<ExpandoObject> GenerateFakeItems(int count)
    {
        // Seed for deterministic tests
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
                dict["InnerClass"] = new ExpandoObject();

                return (ExpandoObject)item;
            })
            .ToList();
    }
}
