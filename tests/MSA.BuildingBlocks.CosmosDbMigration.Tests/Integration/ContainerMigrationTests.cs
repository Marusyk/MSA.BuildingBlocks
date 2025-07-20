using System.Dynamic;
using MSA.BuildingBlocks.CosmosDbMigration.Tests;
using MSA.BuildingBlocks.CosmosDbMigration.Tests.Integration;
using Xunit;

namespace MSA.BuildingBlocks.CosmosDbContainerMigration.Tests;

public sealed class ContainerContainerMigrationTests : IClassFixture<ContainerMigrationTestFixture>
{
    private readonly ContainerMigrationTestFixture _containerMigrationTestFixture;

    public ContainerContainerMigrationTests(ContainerMigrationTestFixture containerMigrationTestFixture)
    {
        _containerMigrationTestFixture = containerMigrationTestFixture;
    }

    [Fact]
    public async Task GetItems_Should_Return_Initially_Inserted_Items()
    {
        // Arrange
        List<ExpandoObject> initialItems = _containerMigrationTestFixture.InitialItems;

        await using ContainerMigrationTestContext context
            = await ContainerMigrationTestContext.CreateAsync(initialItems);

        // Act
        IList<ExpandoObject> actualItems = await context.ContainerMigration.GetItems();

        // Assert
        Assert.Equal(initialItems.Count, actualItems.Count);
    }

    [Fact]
    public async Task UpsertItems_Should_Insert_Initial_Items()
    {
        // Arrange
        List<ExpandoObject> initialItems = _containerMigrationTestFixture.InitialItems;

        await using ContainerMigrationTestContext context
            = await ContainerMigrationTestContext.CreateAsync([]);

        // Act
        await context.ContainerMigration.UpsertItems(initialItems);

        // Assert
        IList<ExpandoObject> actualItems = await context.ContainerMigration.GetItems();
        Assert.Equal(initialItems.Count, actualItems.Count);

        foreach (ExpandoObject item in initialItems)
        {
            IDictionary<string, object> originalDict = ToDictionary(item);
            string id = originalDict["id"]?.ToString();

            Assert.Contains(actualItems, inserted =>
            {
                IDictionary<string, object> insertedDict = ToDictionary(inserted);
                return insertedDict["id"]?.ToString() == id;
            });
        }
    }

    [Fact]
    public async Task RemoveItemsByQuery_Should_Delete_All_Matching_Items()
    {
        // Arrange
        List<ExpandoObject> initialItems = _containerMigrationTestFixture.InitialItems;

        await using ContainerMigrationTestContext context
            = await ContainerMigrationTestContext.CreateAsync(initialItems);

        string query = "SELECT * FROM c WHERE c.SomeField = 'SomeField'";

        IList<ExpandoObject> currentItems = await context.ContainerMigration.GetItems(query);
        Assert.NotEmpty(currentItems);

        // Act
        await context.ContainerMigration.RemoveItemsByQuery(query);

        // Assert
        IList<ExpandoObject> remainingItems = await context.ContainerMigration.GetItems(query);
        Assert.Empty(remainingItems);
    }

    [Fact]
    public async Task AddPropertyToItems_Should_Add_Property_To_The_Root()
    {
        // Arrange
        List<ExpandoObject> initialItems = _containerMigrationTestFixture.InitialItems;

        await using ContainerMigrationTestContext context
            = await ContainerMigrationTestContext.CreateAsync(initialItems);

        string propertyName = "status";
        string value = "active";

        string query = $"SELECT * FROM c WHERE c.{propertyName} = '{value}'";
        IList<ExpandoObject> currentItems = await context.ContainerMigration.GetItems(query);
        Assert.Empty(currentItems);

        IList<ExpandoObject> items = await context.ContainerMigration.GetItems();

        // Act
        await context.ContainerMigration.AddPropertyToItems(items, propertyName, value);

        // Assert
        IList<ExpandoObject> updatedItems = await context.ContainerMigration.GetItems();
        foreach (ExpandoObject item in updatedItems)
        {
            IDictionary<string, object> dict = ToDictionary(item);
            Assert.True(dict.ContainsKey(propertyName));
            Assert.Equal(value, dict[propertyName]);
        }
    }

    [Fact]
    public async Task AddPropertyToItems_Should_Add_Nested_Property_To_The_Existing_Root_Property()
    {
        // Arrange
        List<ExpandoObject> initialItems = _containerMigrationTestFixture.InitialItems;

        await using ContainerMigrationTestContext context
            = await ContainerMigrationTestContext.CreateAsync(initialItems);

        IList<ExpandoObject> items
            = await context.ContainerMigration.GetItems();

        string path = "InnerClass";
        string propertyName = "isProcessed";
        bool value = true;

        string query = $"SELECT * FROM c WHERE c.{path}.{propertyName} = {value.ToString().ToLower()}";
        IList<ExpandoObject> currentItems = await context.ContainerMigration.GetItems(query);
        Assert.Empty(currentItems);

        // Act
        await context.ContainerMigration.AddPropertyToItems(items, path, propertyName, value);

        // Assert
        IList<ExpandoObject> updatedItems = await context.ContainerMigration.GetItems();
        foreach (ExpandoObject item in updatedItems)
        {
            IDictionary<string, object> metadata = GetNestedDictionary(item, path);
            Assert.True(metadata.ContainsKey(propertyName));
            Assert.Equal(value, metadata[propertyName]);
        }
    }

    [Fact]
    public async Task RemovePropertyFromItems_Should_Remove_Root_Property()
    {
        // Arrange
        List<ExpandoObject> initialItems = _containerMigrationTestFixture.InitialItems;

        await using ContainerMigrationTestContext context
            = await ContainerMigrationTestContext.CreateAsync(initialItems);

        IList<ExpandoObject> items = await context.ContainerMigration.GetItems();
        string propertyName = "tempField";
        object value = "removeMe";

        await context.ContainerMigration.AddPropertyToItems(items, propertyName, value);

        string query = $"SELECT * FROM c WHERE c.{propertyName} = '{value}'";
        IList<ExpandoObject> currentItems = await context.ContainerMigration.GetItems(query);
        Assert.NotEmpty(currentItems);

        // Act
        await context.ContainerMigration.RemovePropertyFromItems(items, propertyName);

        // Assert
        IList<ExpandoObject> updatedItems = await context.ContainerMigration.GetItems();
        foreach (ExpandoObject item in updatedItems)
        {
            IDictionary<string, object> dict = ToDictionary(item);
            Assert.False(dict.ContainsKey(propertyName));
        }
    }

    [Fact]
    public async Task RemovePropertyFromItems_Should_Remove_Nested_Property_From_The_Existing_Root_Property()
    {
        // Arrange
        List<ExpandoObject> initialItems = _containerMigrationTestFixture.InitialItems;

        await using ContainerMigrationTestContext context
            = await ContainerMigrationTestContext.CreateAsync(initialItems);

        IList<ExpandoObject> items = await context.ContainerMigration.GetItems();
        string path = "InnerClass";
        string propertyName = "toRemove";
        string value = "value";

        await context.ContainerMigration.AddPropertyToItems(items, path, propertyName, value);

        string query = $"SELECT * FROM c WHERE c.{path}.{propertyName} = '{value}'";
        IList<ExpandoObject> currentItems = await context.ContainerMigration.GetItems(query);
        Assert.NotEmpty(currentItems);

        // Act
        await context.ContainerMigration.RemovePropertyFromItems(items, path, propertyName);

        // Assert
        IList<ExpandoObject> updatedItems = await context.ContainerMigration.GetItems();
        foreach (ExpandoObject item in updatedItems)
        {
            IDictionary<string, object> metadata = GetNestedDictionary(item, path);
            Assert.False(metadata.ContainsKey(propertyName));
        }
    }

    private static IDictionary<string, object> ToDictionary(ExpandoObject obj) =>
        (IDictionary<string, object>)obj;

    private static IDictionary<string, object> GetNestedDictionary(ExpandoObject root, string path)
    {
        IDictionary<string, object> rootDict = ToDictionary(root);
        if (rootDict.TryGetValue(path, out object nested) && nested is ExpandoObject nestedObj)
        {
            return ToDictionary(nestedObj);
        }

        throw new InvalidOperationException($"Nested object '{path}' not found or invalid.");
    }
}
