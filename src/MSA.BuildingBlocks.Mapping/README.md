# MSA.BuildingBlocks.Mapping

## How to use

`This package provides a simple way to automatically map data transfer objects (DTOs) to and from entities in your application. This package uses AutoMapper under the hood to perform the mapping.

## Installation

You can install the MSA.BuildingBlocks.Mapping package via NuGet or through the Visual Studio package manager console:

    PM> Install-Package MSA.BuildingBlocks.Mapping

## Usage

First, add the `IServiceClient` to the dependency injection container in `Startup.cs`:

```csharp
using MSA.BuildingBlocks.ServiceClient;

public void ConfigureServices(IServiceCollection services)
{
    services.AddMappingProfiles();
}
```

### Example

For example, to inject the Mapper into a code:

```csharp
public class MyService
{
    private readonly IMapper _mapper;

    public MyService(IMapper mapper)
    {
        _mapper = mapper;
    }

    public MyClassDto MyMethod(MyClassDto myClassDto)
    {
        var myClass = _mapper.Map<MyClass>(myClassDto); //example of mapping to MyClass
        var myClassDto2 = _mapper.Map<MyClassDto>(myClass); //example of mapping from MyClass        

        return myClassDto2;
    }
}

public class MyClassDto : IMapFrom<MyClass>, IMapTo<MyClass>
{
    public int Id { get; set; }
    public string FullName { get; set; }
}

public class MyClass
{
    public int Id { get; set; }
    public string FullName { get; set; }
}
```