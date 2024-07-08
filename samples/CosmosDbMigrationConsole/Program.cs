using System.Collections.ObjectModel;
using System.Dynamic;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using MSA.BuildingBlocks.CosmosDbMigration;
using Newtonsoft.Json;

Console.WriteLine("Sample migrations started.");

#region Initialize db and variables for sample
string databaseId = "TestDb";
string containerId = "TestContainer1";
CosmosClient cosmosClient = new("AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==");

DatabaseResponse db = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);
await db.Database
    .DefineContainer(containerId, "/CountryCode")
    .CreateIfNotExistsAsync();

using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
#endregion

#region Database migrations
DatabaseMigration databaseMigration = new(
    cosmosClient,
    databaseId: databaseId,
    containerId: containerId,
    logger: factory.CreateLogger<DatabaseMigration>());

Console.WriteLine("Creating container 'TestContainer2'");
await databaseMigration.CreateContainer("TestContainer2", $"{nameof(TestClass.MyProperty2)}");

Console.WriteLine($"Recreating container 'TestContainer2' with partition key {nameof(TestClass.MyProperty)}");
await databaseMigration.SwitchToContainer("TestContainer2");
await databaseMigration.RecreateContainerWithNewPartitionKey($"{nameof(TestClass.MyProperty)}");

Console.WriteLine("Clone container 'TestContainer2' as 'TestContainer3'");
await databaseMigration.CloneContainer("TestContainer3", $"{nameof(TestClass.MyProperty)}");

Console.WriteLine("Delete container 'TestContainer3'");
await databaseMigration.SwitchToContainer("TestContainer3");
await databaseMigration.DeleteContainer();

Console.WriteLine("Replace indexing policy to 'TestContainer2'");
await databaseMigration.SwitchToContainer("TestContainer2");
Collection<IncludedPath> includedPathes =
[
    new IncludedPath
    {
        Path = "/SomeField/?"
    }
];

Collection<ExcludedPath> excludedPathes =
[
    new ExcludedPath()
    {
        Path = "/*"
    }
];

Collection<Collection<CompositePath>> compositePathes =
[
    new()
    {
        new CompositePath()
        {
            Path = "/SomeField",
            Order = CompositePathSortOrder.Ascending
        },
        new CompositePath()
        {
            Path = "/id",
            Order = CompositePathSortOrder.Ascending
        }
    }
];

await databaseMigration.ReplaceIndexingPolicy(includedPathes, excludedPathes, compositePathes);

Console.WriteLine("Add indexing policy to 'TestContainer2'");
includedPathes =
[
    new IncludedPath
    {
        Path = $"/{nameof(TestClass.MyProperty3)}/?"
    }
];
await databaseMigration.AddIndexingPolicy(includedPathes);
#endregion

#region Container migrations
ContainerMigration containerMigration = new(
    cosmosClient,
    databaseId,
    containerId,
    factory.CreateLogger<ContainerMigration>());

Console.WriteLine("Upload dummy data in 'TestContainer2'");
IList<TestClass> dummyData = JsonConvert.DeserializeObject<IList<TestClass>>(File.ReadAllText(@"../../../DummyTestData/1_000_items.json"));
await containerMigration.SwitchToContainer("TestContainer2");
await containerMigration.UpsertItems(dummyData);

Console.WriteLine("Get all items by query");
IList<ExpandoObject> items = await containerMigration.GetItems();

Console.WriteLine("Add property MyProperty6 to all items with value 10");
await containerMigration.AddPropertyToItems(items, "MyProperty6", 10);

Console.WriteLine("Add property InnerClass.SomeProp1 in InnerClass to all items");
await containerMigration.AddPropertyToItems(items, "InnerClass2", new { SomeProp1 = new { SomeProp = "SMTH" } });

Console.WriteLine("Add inner property SomeProp3 with value 'InterProp = Some' to TestClass.InnerClass");
items = await containerMigration.GetItems();
await containerMigration.AddPropertyToItems(items, "InnerClass2.SomeProp1", "SomeProp3", new { SomeProp = 10 });

Console.WriteLine("Remove property MyProperty6 from all items");
await containerMigration.RemovePropertyFromItems(items, "MyProperty6");

Console.WriteLine("Remove inner property SomeProp3 for class TestClass.InnerClass");
await containerMigration.RemovePropertyFromItems(items, "InnerClass2.SomeProp1", "SomeProp3");

Console.WriteLine("Remove items by query 'SELECT * FROM c WHERE c.MyProperty = 1'");
await containerMigration.RemoveItemsByQuery("SELECT * FROM c WHERE c.MyProperty = 1");

Console.WriteLine("Remove container 'TestContainer2'");
await databaseMigration.DeleteContainer();
#endregion 

#region Classes used in migrations
public class TestClass
{
    [JsonProperty(PropertyName = "id")]
    public string id { get; set; }
    public string SomeField { get; set; }
    public int MyProperty { get; set; }
    public int MyProperty2 { get; set; }
    public int MyProperty3 { get; set; }
    public string CountryCode { get; set; }

    public TestInnerClass InnerClass { get; set; }

    public class TestInnerClass
    {
        public TestInner2Class SomeProp1 { get; set; }

        public class TestInner2Class
        {
            public int SomeProp { get; set; }
        }
    }
}
#endregion
