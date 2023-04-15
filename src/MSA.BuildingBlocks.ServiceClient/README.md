# MSA.BuildingBlocks.ServiceClient

## How to use

This package provides a service client for communicating with a remote service via HTTP using .NET 7. It includes options for setting the base URL, authentication, and additional configuration.

## Installation

To install the package, use the following command in the Package Manager Console:

    PM> Install-Package MSA.BuildingBlocks.ServiceClient

## Usage

First, add the `IServiceClient` to the dependency injection container in `Startup.cs`:

```csharp
using MSA.BuildingBlocks.ServiceClient;

public void ConfigureServices(IServiceCollection services)
{
    services.AddServiceClient<IMyService, MyServiceClient>(
        client =>
            {
                client.BaseAddress = new Uri("https://example.com");
            },
    );
}
```
The AddServiceClient method takes the following parameters:

- TClient - The interface for the service client.
- TImplementation - The class that implements the service client.
- host - The base URL for the remote service.
- authToken - The authentication token to use for requests. Note that this is a static token and should be replaced with a dynamic value in production code.
- options - Additional options for configuring the service client. This parameter is optional and can be omitted if no additional options need to be configured.

For example, to inject the service client into a controller:

```csharp
using MSA.BuildingBlocks.ServiceClient;

public class MyController : Controller
{
    private readonly IMyService _serviceClient;

    public MyController(IMyService serviceClient)
    {
        _serviceClient = serviceClient;
    }

    public async Task<IActionResult> Index()
    {
        var result = await _serviceClient.MyMethod();
        return View(result);
    }
}
```

### Example
Here's an example of using the service client to make a request to a remote service:

```cs
using MSA.BuildingBlocks.ServiceClient;

public interface IMyServiceApi
{
    Task<MyResponse> MyMethod();
}

public class MyServiceClientApi : ServiceClientBase, IMyServiceApi
{
    public MyServiceClientApi(HttpClient client, ILoggerFactory loggerFactory, string version)
    : base(client, version, loggerFactory.CreateLogger<ServiceClientBase>())
    {
    }

    public Task<ServiceResponse<MyResponse>> MyMethod(Request request, string token)
    {
        HttpRequestMessage requestMessage = ServiceUri.AbsoluteUri
            .WithHttpMethod(HttpMethod.Post) // HTTP method of the endpoint
            .WithJsonContent(request) // request body
            .WithHeader(HeadersNames.Authorization, token); // for passing authentication token

        return SendAsync<MyResponse>(requestMessage);
    }
}

public class MyResponse
{
    public string Message { get; set; }
}
```

