# MSA.BuildingBlocks

MSA.BuildingBlocks will include the following packages:
 - MSA.BuildingBlocks.EventBus - infrastructure for work with messaging-broker infrastructure technology like Azure Service Bus, RabbitMQ and Kafka
 - MSA.BuildingBlocks.RequestReplyBus - infrastructure for work with messaging-broker infrastructure technology like Azure Service Bus, RabbitMQ and Kafka
 - MSA.BuildingBlocks.FileStorage - infrastructure for work with file storage (Azure Blob storage, Azure DataLake, Amazon S3)
 - MSA.BuildingBlocks.Logging - contains common logging infrastructure that using Serilog with Elasticsearch format and Application Insight sink
 - MSA.BuildingBlocks.Metrics - prometheus metrics extensions
 - MSA.BuildingBlocks.RESTSDK - infrastructure for internal communication between services by HTTP
 - MSA.BuildingBlocks.Tracing - infrastructure for distributed tracing
 - MSA.BuildingBlocks.WebApp - infrastructure for work with web application that based on ASP.NET Core 
 - MSA.BuildingBlocks.Caching - common caching infrastructue
 - MSA.BuildingBlocks.Mapping - extensions and interfaces `IMapTo<>` and `IMapFrom<>` for AutoMapper
 - MSA.BuildingBlocks.CosmosDbMigrations - migration mechanism for CosmosDB
