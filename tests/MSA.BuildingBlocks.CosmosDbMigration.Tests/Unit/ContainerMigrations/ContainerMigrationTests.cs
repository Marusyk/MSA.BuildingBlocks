using System.Dynamic;
using System.Net;
using Bogus;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace MSA.BuildingBlocks.CosmosDbMigration.Tests.Unit;

public sealed class ContainerMigrationTests
{
    private readonly Faker _faker = new();
    private readonly Container _containerMock;
    private readonly ContainerMigration _migration;

    public ContainerMigrationTests()
    {
        Randomizer.Seed = new Random(1234);

        _containerMock = Substitute.For<Container>();
        _containerMock
            .ReadContainerAsync(null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ContainerResponse>(new ContainerResponseStub()));

        CosmosClient cosmosClientMock = Substitute.For<CosmosClient>();
        cosmosClientMock
            .GetContainer(Arg.Any<string>(), Arg.Any<string>())
            .Returns(_containerMock);

        ILogger<ContainerMigration> loggerMock = Substitute.For<ILogger<ContainerMigration>>();

        _migration = new ContainerMigration(
            cosmosClient: cosmosClientMock,
            databaseId: "FakeDb",
            containerId: "FakeContainer",
            logger: loggerMock);
    }

    [Fact]
    public async Task AddPropertyToItems_Should_Add_Property_And_Value_To_The_Root_If_Not_Exists()
    {
        // Arrange
        string propertyName = "status";
        string value = "active";
        string id = Guid.NewGuid().ToString();
        string partitionKey = _faker.Address.CountryCode();

        ExpandoObject item = CreateArrangeItem(id, partitionKey);
        IList<ExpandoObject> items = [item];

        _containerMock
            .ReplaceItemStreamAsync(
                Arg.Any<Stream>(),
                id,
                Arg.Is<PartitionKey>(p => p.ToString().Contains(partitionKey)),
                Arg.Any<ItemRequestOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ResponseMessage(HttpStatusCode.OK)));

        // Act
        await _migration.AddPropertyToItems(items, propertyName, value);

        // Assert
        IDictionary<string, object> dict = item;
        Assert.True(dict.ContainsKey(propertyName));
        Assert.Equal(value, dict[propertyName]);
    }

    [Fact]
    public async Task AddPropertyToItems_Should_Throw_Exception_If_Property_Exists_In_The_Root()
    {
        // Arrange
        string id = Guid.NewGuid().ToString();
        string partitionKey = _faker.Address.CountryCode();

        ExpandoObject item = CreateArrangeItem(id, partitionKey);

        var dict = (IDictionary<string, object>)item;
        dict.Add("existing", "value");
        IList<ExpandoObject> items = [item];

        // Act
        Func<Task> act = () => _migration.AddPropertyToItems(items, "existing", "new");

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task AddPropertyToItems_Nested_Object_Should_Be_Inserted_Into_Nested_Object()
    {
        // Arrange
        string id = Guid.NewGuid().ToString();
        string partitionKey = _faker.Address.CountryCode();

        ExpandoObject item = CreateArrangeItem(id, partitionKey);

        ExpandoObject innerProperty = new();
        ((IDictionary<string, object>)item)["InnerClass"] = innerProperty;
        IList<ExpandoObject> items = [item];

        _containerMock
            .ReplaceItemStreamAsync(
                Arg.Any<Stream>(),
                id,
                Arg.Is<PartitionKey>(p => p.ToString().Contains(partitionKey)),
                Arg.Any<ItemRequestOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ResponseMessage(HttpStatusCode.OK)));

        // Act
        await _migration.AddPropertyToItems(items, "InnerClass", "flag", true);

        // Assert
        var innerDict = (IDictionary<string, object>)innerProperty;
        Assert.True(innerDict.ContainsKey("flag"));
        Assert.Equal(true, innerDict["flag"]);
    }

    [Fact]
    public async Task RemovePropertyFromItems_Should_Remove_Property_From_Root_If_Presented()
    {
        // Arrange
        string id = Guid.NewGuid().ToString();
        string partitionKey = _faker.Address.CountryCode();

        ExpandoObject item = CreateArrangeItem(id, partitionKey);

        IDictionary<string, object> dict = item;
        const string propertyName = "status";
        dict.Add(propertyName, "obsolete");
        IList<ExpandoObject> items = [item];

        _containerMock
            .ReplaceItemStreamAsync(
                Arg.Any<Stream>(),
                id,
                Arg.Is<PartitionKey>(p => p.ToString().Contains(partitionKey)),
                Arg.Any<ItemRequestOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ResponseMessage(HttpStatusCode.OK)));

        // Act
        await _migration.RemovePropertyFromItems(items, propertyName);

        // Assert
        Assert.False(dict.ContainsKey(propertyName));
    }

    [Fact]
    public async Task RemovePropertyFromItems_Should_Remove_Nested_Property_If_Presented()
    {
        // Arrange
        string id = Guid.NewGuid().ToString();
        string partitionKey = _faker.Address.CountryCode();

        ExpandoObject item = CreateArrangeItem(id, partitionKey);
        ExpandoObject nested = new();
        IDictionary<string, object> nestedDict = nested;
        nestedDict["shouldRemove"] = "value";
        ((IDictionary<string, object>)item)["InnerClass"] = nested;
        IList<ExpandoObject> items = [item];

        _containerMock
           .ReplaceItemStreamAsync(
               Arg.Any<Stream>(),
               id,
               Arg.Is<PartitionKey>(p => p.ToString().Contains(partitionKey)),
               Arg.Any<ItemRequestOptions>(),
               Arg.Any<CancellationToken>())
           .Returns(Task.FromResult(new ResponseMessage(HttpStatusCode.OK)));

        // Act
        await _migration.RemovePropertyFromItems(items, "InnerClass", "shouldRemove");

        // Assert
        Assert.False(nestedDict.ContainsKey("shouldRemove"));
    }

    private static ExpandoObject CreateArrangeItem(string id, string partitionKey)
    {
        ExpandoObject item = new();
        IDictionary<string, object> dict = item;
        dict["id"] = id;
        dict["CountryCode"] = partitionKey;
        return item;
    }
}

public sealed class ContainerResponseStub : ContainerResponse
{
    public override ContainerProperties Resource { get; } = new ContainerProperties
    {
        PartitionKeyPath = "/CountryCode"
    };
}
