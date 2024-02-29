<img align="right" width="100" src="block.png" /><img align="right" width="100" src="block.png" /><img align="right" width="100" src="block.png" />

# MSA.BuildingBlocks

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/Marusyk/grok.net/blob/main/LICENSE)
[![contributions welcome](https://img.shields.io/badge/contributions-welcome-brightgreen.svg?style=flat)](https://github.com/Marusyk/grok.net/blob/main/CONTRIBUTING.md)

**MSA.BuildingBlocks** is a collection of common building blocks designed to aid the development of microservice-based applications using .NET. This library provides a variety of packages, each offering specific functionalities that cater to different aspects of microservice architecture.

Packages
The MSA.BuildingBlocks library includes the following packages:

* [x] [**MSA.BuildingBlocks.ServiceClient**](src/MSA.BuildingBlocks.ServiceClient/README.md): Provides infrastructure for internal HTTP communication between services.
* [x] [**MSA.BuildingBlocks.Mapping**](src/MSA.BuildingBlocks.Mapping/README.md): Contains extensions and interfaces IMapTo<> and IMapFrom<> for AutoMapper, facilitating object-object mapping.
* [ ] **MSA.BuildingBlocks.CosmosDbMigrations**: Implements a migration mechanism for CosmosDB, ensuring your database schema is always up-to-date.
* [ ] **MSA.BuildingBlocks.EventBus**: Offers infrastructure for working with messaging-broker technologies like Azure Service Bus, RabbitMQ, and Kafka.
* [ ] **MSA.BuildingBlocks.RequestReplyBus**: Provides infrastructure for working with messaging-broker technologies like Azure Service Bus and RabbitMQ, specifically tailored for request-reply messaging patterns.
* [ ] **MSA.BuildingBlocks.FileStorage**: Facilitates work with file storage systems such as Azure Blob storage, Azure DataLake, and Amazon S3.
* [ ] **MSA.BuildingBlocks.Logging**: Contains a common logging infrastructure that uses Serilog with Elasticsearch format and Application Insight sink.
* [ ] **MSA.BuildingBlocks.Metrics**: Provides extensions for Prometheus metrics.
* [ ] **MSA.BuildingBlocks.Tracing**: Implements infrastructure for distributed tracing, improving observability in microservice architectures.
* [ ] **MSA.BuildingBlocks.WebApp**: Provides infrastructure for working with web applications based on ASP.NET Core.
* [ ] **MSA.BuildingBlocks.Caching**: Implements a common caching infrastructure, improving performance by reducing unnecessary database calls.


## How to Install

Install as a library from [Nuget](https://www.nuget.org/packages?q=MSA.BuildingBlocks):

**[MSA.BuildingBlocks.Mapping](https://www.nuget.org/packages/MSA.BuildingBlocks.Mapping/)** 

[![NuGet version](https://img.shields.io/nuget/v/MSA.BuildingBlocks.Mapping.svg?logo=NuGet)](https://www.nuget.org/packages/MSA.BuildingBlocks.Mapping)
[![Nuget](https://img.shields.io/nuget/dt/MSA.BuildingBlocks.Mapping.svg)](https://www.nuget.org/packages/MSA.BuildingBlocks.Mapping)

    PM> Install-Package MSA.BuildingBlocks.Mapping

**[MSA.BuildingBlocks.ServiceClient](https://www.nuget.org/packages/MSA.BuildingBlocks.ServiceClient/)**

[![NuGet version](https://img.shields.io/nuget/v/MSA.BuildingBlocks.ServiceClient.svg?logo=NuGet)](https://www.nuget.org/packages/MSA.BuildingBlocks.ServiceClient)
[![Nuget](https://img.shields.io/nuget/dt/MSA.BuildingBlocks.ServiceClient.svg)](https://www.nuget.org/packages/MSA.BuildingBlocks.ServiceClient)

    PM> Install-Package MSA.BuildingBlocks.ServiceClient
