// See https://aka.ms/new-console-template for more information
using Microsoft.Azure.Cosmos;
using MSA.BuildingBlocks.CosmosDbMigration.Abstractions;
using MSA.BuildingBlocks.CosmosDbMigration.NoSQL;
using System;
using System.Collections.ObjectModel;

Console.WriteLine("Testing migrations");

BaseDatabaseMigration databaseMigration = new DatabaseMigration(
    new CosmosClient("AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="),
    "TestDb",
    "TestContainer1");


//var response =  await migrationOperation.GetItems();

//await migrationOperation.RecreateContainerWithNewPartitionKey("CountryCode");

//await databaseMigration.CloneContainer("TestContainer3", "id");

Collection<IncludedPath> includedPathes = new()
{
    new IncludedPath
    {
        Path = "/FirstName/?"
    }
};

Collection<ExcludedPath> excludedPathes = new()
{
    new ExcludedPath()
    {
        Path = "/*"
    }
};

var compositePathes = new Collection<Collection<CompositePath>>()
{
    new Collection<CompositePath>()
    {
        new CompositePath()
        {
            Path = "/FirstName",
            Order = CompositePathSortOrder.Ascending
        },
        new CompositePath()
        {
            Path = "/id",
            Order = CompositePathSortOrder.Ascending
        }
    }
};


await databaseMigration.AddIndexingPolicy(includedPathes, excludedPathes, compositePathes);

//await databaseMigration.SwitchToContainer("Analysis", "BankPropositionsPredictor");
//await databaseMigration.CloneContainer("Analysis2", "Suggestion");



Console.ReadLine();