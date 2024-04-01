using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using MSA.BuildingBlocks.CosmosDbMigration;
using Newtonsoft.Json;

Console.WriteLine("Testing migrations");

using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());

//BaseDatabaseMigration databaseMigration = new DatabaseMigration(
//    new CosmosClient("AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="),
//    "TestDb",
//    "TestContainer1",
//    factory.CreateLogger<DatabaseMigration>());

////var response =  await migrationOperation.GetItems();

////await migrationOperation.RecreateContainerWithNewPartitionKey("CountryCode");

////await databaseMigration.CloneContainer("TestContainer3", "id");

//Collection<IncludedPath> includedPathes = new()
//{
//    new IncludedPath
//    {
//        Path = "/FirstName/?"
//    }
//};

//Collection<ExcludedPath> excludedPathes = new()
//{
//    new ExcludedPath()
//    {
//        Path = "/*"
//    }
//};

//var compositePathes = new Collection<Collection<CompositePath>>()
//{
//    new Collection<CompositePath>()
//    {
//        new CompositePath()
//        {
//            Path = "/FirstName",
//            Order = CompositePathSortOrder.Ascending
//        },
//        new CompositePath()
//        {
//            Path = "/id",
//            Order = CompositePathSortOrder.Ascending
//        }
//    }
//};

//await databaseMigration.AddIndexingPolicy(includedPathes, excludedPathes, compositePathes);

//await databaseMigration.SwitchToContainer("Analysis", "BankPropositionsPredictor");
//await databaseMigration.CloneContainer("Analysis2", "Suggestion");

BaseContainerMigration containerMigration = new ContainerMigration(
    new CosmosClient("AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="),
    "TestDb",
    "TestContainer1",
    factory.CreateLogger<ContainerMigration>());

//var items = await containerMigration.GetItems();

//await containerMigration.UpsertItems<TestClass>(new List<TestClass>
//{
//    new TestClass
//    {
//        id = Guid.NewGuid().ToString(),
//        MyProperty = 10002,
//        MyProperty2 = 4,
//        MyProperty3 = 3,
//        CountryCode = "UA"
//    },
//    new TestClass
//    {
//        id = Guid.NewGuid().ToString(),
//        MyProperty = 1,
//        MyProperty2 = 2,
//        MyProperty3 = 3,
//        CountryCode = "FR"
//    },
//    new TestClass
//    {
//        id = Guid.NewGuid().ToString(),
//        MyProperty = 1,
//        MyProperty2 = 2,
//        MyProperty3 = 3,
//        CountryCode = "TR"
//    }
//});

//await containerMigration.UpsertItem<TestClass>(new TestClass
//{
//    id = Guid.NewGuid().ToString(),
//    MyProperty = 10002,
//    MyProperty2 = 1234,
//    MyProperty3 = 3,
//    CountryCode = "UA"
//});

//await containerMigration.AddPropertyToItems(items, "InnerClass.SomeProp1", "SomeProp3", new { InterProp = "Some" });
//await containerMigration.AddPropertyToItems(items, "MyProperty6", 10);
//await containerMigration.RemovePropertyFromItems(items, "InnerClass.SomeProp1", "SomeProp3");
//await containerMigration.RemovePropertyFromItems(items, "MyProperty6");

await containerMigration.RemoveItemsByQuery("SELECT * FROM c WHERE c.CountryCode = \"AR\" and c.City = \"Necochea\"");

Console.ReadLine();

public class TestClass
{
    [JsonProperty(PropertyName = "id")]
    public string id { get; set; }
    public string someField { get; set; }
    public int MyProperty { get; set; }
    public int MyProperty2 { get; set; }
    public int MyProperty3 { get; set; }
    public string CountryCode { get; set; }

    public TestInnerClass InnerClass { get; set; }
}

public class TestInnerClass
{
    public TestInner2Class SomeProp1 { get; set; }

    public class TestInner2Class
    {
        public int SomeProp { get; set; }
    }
}
