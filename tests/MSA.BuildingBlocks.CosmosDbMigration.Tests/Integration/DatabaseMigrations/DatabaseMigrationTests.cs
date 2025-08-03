using System.Collections.ObjectModel;
using System.Dynamic;
using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Xunit;

namespace MSA.BuildingBlocks.CosmosDbMigration.Tests.Integration.DatabaseMigrations;

public class DatabaseMigrationTests(MigrationTestFixture fixture)
    : IClassFixture<MigrationTestFixture>
{
    private readonly MigrationTestFixture _fixture = fixture;

    [Fact]
    public async Task CloneContainer_Shoud_Copy_All_Items_To_The_New_Container()
    {
        // Arrange
        List<ExpandoObject> initialItems = _fixture.InitialItems;

        await using MigrationTestContext<DatabaseMigration> context
            = await MigrationTestContext<DatabaseMigration>.CreateAsync(
                (client, db, container, logger) => new DatabaseMigration(client, db, container, logger),
                initialItems);

        string cloneContainerId = $"TestContainer_clone_{Guid.NewGuid()}";

        // Act
        await context.Migration.CloneContainer(cloneContainerId, context.PartitionKey);

        // Assert
        Container cloneContainer = context.Database.GetContainer(cloneContainerId);
        var countQuery = new QueryDefinition("SELECT VALUE COUNT(1) FROM c");
        int clonedCount = await cloneContainer
            .GetItemLinqQueryable<int>(requestOptions: new QueryRequestOptions { MaxItemCount = 1 })
            .CountAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(initialItems.Count, clonedCount);
    }

    [Fact]
    public async Task DeleteContainer_Should_Delete_The_Current_Container()
    {
        // Arrange
        await using MigrationTestContext<DatabaseMigration> context = await MigrationTestContext<DatabaseMigration>
            .CreateAsync(
                (client, db, container, logger) => new DatabaseMigration(client, db, container, logger),
                seedItems: []);

        // Act
        await context.Migration.DeleteContainer();

        // Assert
        Container deletedContextContainer = context.Container;

        CosmosException ex = await Assert.ThrowsAsync<CosmosException>(
            () => deletedContextContainer.ReadContainerAsync(cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
    }

    [Fact]
    public async Task CreateContainer_Should_Create_A_New_Container_If_Not_Exists()
    {
        // Arrange
        await using MigrationTestContext<DatabaseMigration> context = await MigrationTestContext<DatabaseMigration>
            .CreateAsync(
                (client, db, container, logger) => new DatabaseMigration(client, db, container, logger),
                seedItems: []);

        string newContainerId = $"TestContainer_new_{Guid.NewGuid()}";

        // Act
        await context.Migration.CreateContainer(
            newContainerId,
            context.PartitionKey);

        // Assert
        Container newContainer = context.Database.GetContainer(newContainerId);

        ContainerResponse response = await newContainer.ReadContainerAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(newContainerId, response.Resource.Id);
    }

    [Fact]
    public async Task RecreateContainerWithNewPartitionKey_Should_Recreate_Current_Container_With_Existing_Items_And_Changed_Partition_Key()
    {
        // Arrange
        List<ExpandoObject> initialItems = _fixture.InitialItems;
        await using MigrationTestContext<DatabaseMigration> context = await MigrationTestContext<DatabaseMigration>
            .CreateAsync(
                (client, db, container, logger) => new DatabaseMigration(client, db, container, logger),
                seedItems: initialItems);

        string newPartitionKey = "PostalCode";

        // Act
        await context.Migration.RecreateContainerWithNewPartitionKey(newPartitionKey);

        // Assert
        ContainerProperties props = await context.Container.ReadContainerAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal($"/{newPartitionKey}", props.PartitionKeyPath);

        int actualCount = await context.Container
            .GetItemLinqQueryable<ExpandoObject>()
            .CountAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(initialItems.Count, actualCount);
    }

    [Fact]
    public async Task ReplaceIndexingPolicy_Should_Overwrite_All_Provided_Indexes()
    {
        // Arrange
        await using MigrationTestContext<DatabaseMigration> context = await MigrationTestContext<DatabaseMigration>
            .CreateAsync(
                (client, db, container, logger) => new DatabaseMigration(client, db, container, logger),
                seedItems: []);

        Collection<IncludedPath> includedIndexesOnReplace = [new() { Path = "/SomeField/?" }];
        Collection<ExcludedPath> excludedIndexesOnReplace = [new() { Path = "/*" }];

        // there should be 2 elements in the collection. Otherwise cosmos db will return 400.
        Collection<Collection<CompositePath>> compositeIndexesOnReplace =
        [
            [
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
            ]
        ];

        // Act
        await context.Migration.ReplaceIndexingPolicy(
            includedPaths: includedIndexesOnReplace,
            excludedPaths: excludedIndexesOnReplace,
            compositePaths: compositeIndexesOnReplace);

        // Assert
        ContainerResponse container = await context.Container.ReadContainerAsync(cancellationToken: TestContext.Current.CancellationToken);
        IndexingPolicy policy = container.Resource.IndexingPolicy;

        Assert.Single(policy.IncludedPaths);
        Assert.Equal(includedIndexesOnReplace[0].Path, policy.IncludedPaths[0].Path);

        Assert.Equal(excludedIndexesOnReplace[0].Path, policy.ExcludedPaths[0].Path);

        Assert.Single(policy.CompositeIndexes);
        Assert.Equal(compositeIndexesOnReplace[0][0].Path, policy.CompositeIndexes[0][0].Path);
        Assert.Equal(compositeIndexesOnReplace[0][0].Order, policy.CompositeIndexes[0][0].Order);
        Assert.Equal(compositeIndexesOnReplace[0][1].Path, policy.CompositeIndexes[0][1].Path);
        Assert.Equal(compositeIndexesOnReplace[0][1].Order, policy.CompositeIndexes[0][1].Order);
    }

    public static TheoryData<
        Collection<IncludedPath>,
        Collection<ExcludedPath>,
        Collection<Collection<CompositePath>>,
        string[],
        string[],
        List<List<string>>> TestData { get; } = new()
        {
            // Case 1: only IncludedPaths
            {
                new Collection<IncludedPath>
                {
                    new() { Path = "/exists/?" },
                    new() { Path = "/new/?" }
                },
                null,
                null,
                new[] { "/*", "/exists/?", "/new/?" },
                new[] {"/\"_etag\"/?" }, // This is the default cosmod db index
                new List<List<string>>()
            },

            // Case 2: only ExcludedPaths
            {
                null,
                new Collection<ExcludedPath>
                {
                    new() { Path = "/ex1/*" },
                    new() { Path = "/ex2/*" }
                },
                null,
                new[] { "/*" },// This is the default cosmod db index
                new[] { "/ex1/*", "/ex2/*", "/\"_etag\"/?" },
                new List<List<string>>()
            },

            // Case 3: only CompositePaths
            {
                null,
                null,
                new Collection<Collection<CompositePath>>
                {
                    // there should be 2 elements in the collection. Otherwise cosmos db will return 400.
                    new()
                    {
                        new CompositePath
                        {
                            Path = "/c1",
                            Order = CompositePathSortOrder.Ascending
                        },
                        new CompositePath
                        {
                            Path = "/c2",
                            Order = CompositePathSortOrder.Descending
                        }
                    }
                },
                new[] { "/*" }, // This is the default cosmod db index
                new[] {"/\"_etag\"/?" }, // This is the default cosmod db index
                new List<List<string>>
                {
                    new() { "/c1", "/c2" }
                }
            }
        };

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task AddIndexingPolicy_Should_Add_Only_New_Indexes(
            Collection<IncludedPath> toAddIncluded,
            Collection<ExcludedPath> toAddExcluded,
            Collection<Collection<CompositePath>> toAddComposite,
            string[] expectedIncludedPaths,
            string[] expectedExcludedPaths,
            List<List<string>> expectedCompositePaths)

    {
        await using MigrationTestContext<DatabaseMigration> context = await MigrationTestContext<DatabaseMigration>.CreateAsync(
                (client, db, container, logger) => new DatabaseMigration(client, db, container, logger),
                seedItems: []);

        // Act
        await context.Migration.AddIndexingPolicy(
            includedPaths: toAddIncluded,
            excludedPaths: toAddExcluded,
            compositePaths: toAddComposite);

        // Assert
        ContainerResponse containerResponse = await context.Container.ReadContainerAsync(cancellationToken: TestContext.Current.CancellationToken);
        IndexingPolicy policy = containerResponse.Resource.IndexingPolicy;

        string[] actualIncluded = [..policy.IncludedPaths.Select(p => p.Path)];

        Assert.Equal(expectedIncludedPaths, actualIncluded);

        string[] actualExcluded = [.. policy.ExcludedPaths
                                   .Select(p => p.Path)
                                   .OrderBy(x => x)];

        Assert.Equal(expectedExcludedPaths.OrderBy(x => x), actualExcluded);

        // Verify CompositeIndexes
        var actualComposite = policy.CompositeIndexes
                                    .Select(c => c.Select(cp => cp.Path).ToList())
                                    .ToList();

        Assert.Equal(expectedCompositePaths.Count, actualComposite.Count);
        for (int i = 0; i < expectedCompositePaths.Count; i++)
        {
            Assert.Equal(expectedCompositePaths[i], actualComposite[i]);
        }
    }
}
