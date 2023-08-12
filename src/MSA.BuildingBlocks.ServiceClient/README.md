# MSA.BuildingBlocks.ServiceClient

## How to use

This package provides a service client for communicating with a remote service via HTTP using .NET 7. It includes options for setting the base URL, authentication, and additional configuration.

## Installation

To install the package, use the following command in the Package Manager Console:

    PM> Install-Package MSA.BuildingBlocks.ServiceClient

## Usage
<details open> <summary>For package version 0.1.0</summary>
    
First, add the `ServiceClient` to the dependency injection container in `Startup.cs`:

```csharp
using MSA.BuildingBlocks.ServiceClient;

public void ConfigureServices(IServiceCollection services)
{
    services.AddHttpClient<IMyService, MyServiceClient>(
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
- authToken - The authentication token to use for requests. Note that this is a static token and should be replaced with a dynamic value in the production code.

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

```csharp
using MSA.BuildingBlocks.ServiceClient;

public interface IMyServiceApi
{
    Task<MyResponse> MyMethod();
}

public class MyServiceClientApi : ServiceClientBase, IMyServiceApi
{
    private const string ApiVersion = "v1";
    private const string MyServiceSegment = "";

    public MyServiceClientApi(HttpClient client, ILoggerFactory loggerFactory, string version)
    : base(client, ApiVersion, loggerFactory.CreateLogger<ServiceClientBase>())
    {
    }

    public Task<ServiceResponse<MyResponse>> MyMethod(Request request, string token)
    {
        HttpRequestMessage requestMessage = ServiceUri.AbsoluteUri
            .AppendPathSegments(MyServiceSegment) // appending segments of the resource for a request
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
</details>

<details open> <summary>For package version 1.0.0</summary>
    
In version 1.0.0 injecting is made with `AddServiceClient` to the dependency injection container in `Startup.cs`:

```csharp
using MSA.BuildingBlocks.ServiceClient;

public void ConfigureServices(IServiceCollection services)
{
    services.AddServiceClient<IMyService, MyServiceClient>(
        client =>
            {
                client.BaseAddress = new Uri("https://example.com");
            }
        new ServiceClientOptions ()
            {
                CircuitBreakerPolicy = new ClientCircuitBreakerPolicy { ExceptionsAllowedBeforeBreaking = 3, DurationOfBreakSeconds = 180 },
                RetryPolicy = new ClientRetryPolicy { MaxRetryCount = 3, MedianFirstDelayRetrySeconds = 1 }
            },
    );
}
```
The AddServiceClient method takes the following parameters:

- TClient - The interface for the service client.
- TImplementation - The class that implements the service client.
- host - The base URL for the remote service.
- authToken - The authentication token to use for requests. Note that this is a static token and should be replaced with a dynamic value in the production code.
- options - Additional options for configuring the service client. This parameter is optional and can be omitted if no additional options need to be configured.

Injecting the service client into a controller is the same as for 0.1.0 version of the package.
The major difference from 0.1.0 is added Retry Policy and Circuit Breaker policy which could be added by adding 'AddServiceClient' to the dependency injection container.

</details>
