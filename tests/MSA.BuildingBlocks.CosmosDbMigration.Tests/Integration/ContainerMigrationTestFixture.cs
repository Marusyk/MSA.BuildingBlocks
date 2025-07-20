using System.Dynamic;
using Newtonsoft.Json;

namespace MSA.BuildingBlocks.CosmosDbMigration.Tests.Integration;

public sealed class ContainerMigrationTestFixture
{
    public List<ExpandoObject> InitialItems { get; }

    public ContainerMigrationTestFixture()
    {
        string baseDir = AppContext.BaseDirectory;
        string dataPath = Path.Combine(baseDir, "Integration", "Data", "initial_10_items.json");
        string content = File.ReadAllText(dataPath);

        InitialItems = JsonConvert.DeserializeObject<List<ExpandoObject>>(content)
            ?? throw new InvalidOperationException("Test data could not be loaded.");
    }
}
