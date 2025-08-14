using System.Dynamic;
using Xunit;

namespace MSA.BuildingBlocks.CosmosDbMigration.Tests.Integration.ContainerMigrations;

public sealed class ContainerMigrationTests
    : IClassFixture<MigrationTestFixture>
{
    private readonly MigrationTestFixture _fixture;

    public ContainerMigrationTests(MigrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetItems_Should_Return_Initially_Inserted_Items()
    {
        // Arrange
        List<ExpandoObject> initialItems = _fixture.InitialItems;

        await using MigrationTestContext<ContainerMigration> context
            = await MigrationTestContext<ContainerMigration>.CreateAsync(
                (client, db, container, logger) => new ContainerMigration(client, db, container, logger),
                initialItems);

        // Act
        IList<ExpandoObject> actualItems = await context.Migration.GetItems();

        // Assert
        Assert.Equal(initialItems.Count, actualItems.Count);
    }

    [Fact]
    public async Task UpsertItems_Should_Insert_Initial_Items()
    {
        // Arrange
        List<ExpandoObject> initialItems = _fixture.InitialItems;

        await using MigrationTestContext<ContainerMigration> context
            = await MigrationTestContext<ContainerMigration>.CreateAsync(
                (client, db, container, logger) => new ContainerMigration(client, db, container, logger),
                seedItems: []);

        // Act
        await context.Migration.UpsertItems(initialItems);

        // Assert
        IList<ExpandoObject> actualItems = await context.Migration.GetItems();
        Assert.Equal(initialItems.Count, actualItems.Count);

        foreach (ExpandoObject item in initialItems)
        {
            Dictionary<string, object> originalDict = item.ToDictionary();
            string id = originalDict["id"]?.ToString();

            Assert.Contains(actualItems, inserted =>
            {
                Dictionary<string, object> insertedDict = inserted.ToDictionary();
                return insertedDict["id"]?.ToString() == id;
            }); 
        }
    }

    [Fact]
    public async Task RemoveItemsByQuery_Should_Delete_All_Matching_Items()
    {
        // Arrange
        List<ExpandoObject> initialItems = _fixture.InitialItems;

        await using MigrationTestContext<ContainerMigration> context
            = await MigrationTestContext<ContainerMigration>.CreateAsync(
                (client, db, container, logger) => new ContainerMigration(client, db, container, logger),
                initialItems);

        string query = "SELECT * FROM c WHERE c.SomeField = 'SomeField'";

        IList<ExpandoObject> currentItems = await context.Migration.GetItems(query);
        Assert.NotEmpty(currentItems);

        // Act
        await context.Migration.RemoveItemsByQuery(query);

        // Assert
        IList<ExpandoObject> remainingItems = await context.Migration.GetItems(query);
        Assert.Empty(remainingItems);
    }

    [Fact]
    public async Task AddPropertyToItems_Should_Add_Property_To_The_Root()
    {
        // Arrange
        List<ExpandoObject> initialItems = _fixture.InitialItems;

        await using MigrationTestContext<ContainerMigration> context
            = await MigrationTestContext<ContainerMigration>.CreateAsync(
                (client, db, container, logger) => new ContainerMigration(client, db, container, logger),
                initialItems);

        string propertyName = "status";
        string value = "active";

        string query = $"SELECT * FROM c WHERE c.{propertyName} = '{value}'";
        IList<ExpandoObject> currentItems = await context.Migration.GetItems(query);
        Assert.Empty(currentItems);

        IList<ExpandoObject> items = await context.Migration.GetItems();

        // Act
        await context.Migration.AddPropertyToItems(items, propertyName, value);

        // Assert
        IList<ExpandoObject> updatedItems = await context.Migration.GetItems();
        foreach (ExpandoObject item in updatedItems)
        {
            Dictionary<string, object> dict = item.ToDictionary();
            Assert.True(dict.ContainsKey(propertyName));
            Assert.Equal(value, dict[propertyName]);
        }
    }

    [Fact]
    public async Task AddPropertyToItems_Should_Add_Nested_Property_To_The_Existing_Root_Property()
    {
        // Arrange
        List<ExpandoObject> initialItems = _fixture.InitialItems;

        await using MigrationTestContext<ContainerMigration> context
            = await MigrationTestContext<ContainerMigration>.CreateAsync(
                (client, db, container, logger) => new ContainerMigration(client, db, container, logger),
                initialItems);

        IList<ExpandoObject> items = await context.Migration.GetItems();

        string path = "InnerClass";
        string propertyName = "isProcessed";
        bool value = true;

        string query = $"SELECT * FROM c WHERE c.{path}.{propertyName} = {value.ToString().ToLower()}";
        IList<ExpandoObject> currentItems = await context.Migration.GetItems(query);
        Assert.Empty(currentItems);

        // Act
        await context.Migration.AddPropertyToItems(items, path, propertyName, value);

        // Assert
        IList<ExpandoObject> updatedItems = await context.Migration.GetItems();
        foreach (ExpandoObject item in updatedItems)
        {
            Dictionary<string, object> metadata = GetNestedDictionary(item, path);
            Assert.True(metadata.ContainsKey(propertyName));
            Assert.Equal(value, metadata[propertyName]);
        }
    }

    [Fact]
    public async Task RemovePropertyFromItems_Should_Remove_Root_Property()
    {
        // Arrange
        List<ExpandoObject> initialItems = _fixture.InitialItems;

        await using MigrationTestContext<ContainerMigration> context
            = await MigrationTestContext<ContainerMigration>.CreateAsync(
                (client, db, container, logger) => new ContainerMigration(client, db, container, logger),
                initialItems);

        IList<ExpandoObject> items = await context.Migration.GetItems();
        string propertyName = "tempField";
        object value = "removeMe";

        await context.Migration.AddPropertyToItems(items, propertyName, value);

        string query = $"SELECT * FROM c WHERE c.{propertyName} = '{value}'";
        IList<ExpandoObject> currentItems = await context.Migration.GetItems(query);
        Assert.NotEmpty(currentItems);

        // Act
        await context.Migration.RemovePropertyFromItems(items, propertyName);

        // Assert
        IList<ExpandoObject> updatedItems = await context.Migration.GetItems();
        foreach (ExpandoObject item in updatedItems)
        {
            Dictionary<string, object> dict = item.ToDictionary();
            Assert.False(dict.ContainsKey(propertyName));
        }
    }

    [Fact]
    public async Task RemovePropertyFromItems_Should_Remove_Nested_Property_From_The_Existing_Root_Property()
    {
        // Arrange
        List<ExpandoObject> initialItems = _fixture.InitialItems;

        await using MigrationTestContext<ContainerMigration> context
            = await MigrationTestContext<ContainerMigration>.CreateAsync(
                (client, db, container, logger) => new ContainerMigration(client, db, container, logger),
                initialItems);

        IList<ExpandoObject> items = await context.Migration.GetItems();
        string path = "InnerClass";
        string propertyName = "toRemove";
        string value = "value";

        await context.Migration.AddPropertyToItems(items, path, propertyName, value);

        string query = $"SELECT * FROM c WHERE c.{path}.{propertyName} = '{value}'";
        IList<ExpandoObject> currentItems = await context.Migration.GetItems(query);
        Assert.NotEmpty(currentItems);

        // Act
        await context.Migration.RemovePropertyFromItems(items, path, propertyName);

        // Assert
        IList<ExpandoObject> updatedItems = await context.Migration.GetItems();
        foreach (ExpandoObject item in updatedItems)
        {
            Dictionary<string, object> metadata = GetNestedDictionary(item, path);
            Assert.False(metadata.ContainsKey(propertyName));
        }
    }

    private static Dictionary<string, object> GetNestedDictionary(ExpandoObject root, string path)
    {
        Dictionary<string, object> rootDict = root.ToDictionary();
        if (rootDict.TryGetValue(path, out object nested) && nested is ExpandoObject nestedObj)
        {
            return nestedObj.ToDictionary();
        }

        throw new InvalidOperationException($"Nested object '{path}' not found or invalid.");
    }
}
