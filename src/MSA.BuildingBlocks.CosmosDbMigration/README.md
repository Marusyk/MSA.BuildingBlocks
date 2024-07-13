# MSA.BuildingBlocks.CosmosDbMigration
> :warning: Currently the package is in the alpha version and the development is in progress. 

This README describes the MSA.BuildingBlocks.CosmosDbMigration package designed to simplify Cosmos DB database/container migrations.

## Introduction
This package provides a set of migration operations on Cosmos DB database and container. 
The sample console with migrations could be found [here](https://github.com/Marusyk/MSA.BuildingBlocks/blob/main/samples/CosmosDbMigrationConsole/Program.cs).

**Note:** All operations on the data will cost [RUs](https://learn.microsoft.com/en-us/azure/cosmos-db/request-units). Make all of these operations carefully.

## How to use
Install the package using the NuGet Package Manager:
``` 
PM> Install-Package MSA.BuildingBlocks.CosmosDbMigration
```

**Optional**: Provide a logger where will be logged provided operations with RU costs. For current example console logger is used.<br>
``` cs
using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
```

Create a migration for database:
``` cs 
DatabaseMigration databaseMigration = new(
    cosmosClient: cosmosClient,
    databaseId: databaseId,
    containerId: containerId,
    logger: factory.CreateLogger<DatabaseMigration>()); 
```

for container:<br>
``` cs
ContainerMigration containerMigration = new(
    cosmosClient: cosmosClient,
    databaseId: databaseId,
    containerId: containerId,
    logger: factory.CreateLogger<ContainerMigration>()); 
```

### Current operations
**Container operations**
| Operation | Description | Parameters |
|-----------|-------------|------------|
| GetItems |Retrieves a list of items from the container using a provided SQL query.|-query (Default is "SELECT * FROM c"): The SQL query to execute.|
|SwitchToContainer|	Switches the target container to a different container within the Cosmos DB.|	- containerId: The ID of the new target container.<br> - databaseId: ID of the database containing the new target container.|
|UpsertItems|	Upserts a list of items into the target container.|	- items: The list of items to upsert (type can vary depending on implementation).|
|RemoveItemsByQuery|	Removes items from the container based on a provided SQL query.|	- query: The SQL query to identify items for removal.|
|AddPropertyToItems (Root Property)|	Adds a  new property from a provided list.|	- items: The list of items to modify.<br> - propertyName (string): The name of the property to add.<br> - value: The value to assign to the new property.|
|AddPropertyToItems (Nested Property)|	Adds a new property from a provided list targeting a specific nested location.|	- items : The list of items to modify.<br> - propertyPath: Path to a nested property within the object (defaults to root level).<br> - propertyName: The name of the property to add within the specified path.<br> - value: The value to assign to the new property.|
|RemovePropertyFromItems (Root Property)|	Removes a property from a provided list.|- items: The list of items to modify.<br> - propertyName: The name of the property to remove.|
|RemovePropertyFromItems (Nested Property)|	Removes a property from a provided list targeting a specific nested location.|	- items: The list of items to modify.<br> - propertyPath: The path to the nested property to remove.<br> - propertyName: The name of the property to remove within the specified path.|

Database operations
| Operation | Description | Parameters |
|-----------|-------------|------------|
|CloneContainer|Creates a new container as a clone of the existing target container, including its data and indexing policy.|	- containerId: The ID for the new container. - partitionKey: The partition key path for the new container.|
|CreateContainer|Creates a new container within the current database with the specified ID and partition key path.|	- containerId: The ID for the new container. - partitionKey: The partition key path for the new container.|
DeleteContainer| Deletes the target container.|	- None|
|RecreateContainerWithNewPartitionKey|	Recreates the target container with a new specified partition key path. Data and indexing policy are not preserved.|	- partitionKey: The new partition key path for the target container.|
|AddIndexingPolicy|	Updates the indexing policy for the target container.|	- includedPaths : Collection of included paths for indexing.<br> - excludedPaths : Collection of excluded paths for indexing.<br> - compositePaths: Collection of composite paths for indexing.|
|ReplaceIndexingPolicy|	Replaces the existing indexing policy for the target container.|	- includedPaths: Collection of included paths for indexing.<br> - excludedPaths: Collection of excluded paths for indexing.<br> - compositePaths: Collection of composite paths for indexing.|
|SwitchToContainer|	Switches the target container to a different container within the Cosmos DB.|	- containerId: The ID of the new target container.<br> - databaseId: ID of the database containing the new target container.|

## Limitations
1. Target database and container should exist before any operation.
2. AddPropertyToItems (Nested Property) operation should be provided to a property which is not null.

## License
This project is licensed under the [MIT License](https://opensource.org/license/mit).
