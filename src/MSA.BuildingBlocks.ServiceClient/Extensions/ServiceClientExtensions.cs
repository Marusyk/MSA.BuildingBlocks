using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Contrib.WaitAndRetry;

namespace MSA.BuildingBlocks.ServiceClient;

/// <summary>
/// Provides extension methods for registering service clients with an <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceClientExtensions
{
    /// <summary>
    /// Registers a service client and its implementation with the specified <paramref name="services"/> collection using the specified <paramref name="host"/>, <paramref name="authToken"/>, and <paramref name="options"/>.
    /// </summary>
    /// <typeparam name="TClient">The type of service client to register.</typeparam>
    /// <typeparam name="TImplementation">The implementation type of the service client to register.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to register the service client with.</param>
    /// <param name="host">The <see cref="Uri"/> of the service endpoint.</param>
    /// <param name="authToken">The <see cref="AuthenticationHeaderValue"/> used to authenticate requests to the service.</param>
    /// <param name="options">The <see cref="ServiceClientOptions"/> to configure the service client.</param>
    /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="host"/>, <paramref name="authToken"/>, or <paramref name="options"/> is null.</exception>
    public static IServiceCollection AddServiceClient<TClient, TImplementation>(this IServiceCollection services,
        Uri host, AuthenticationHeaderValue authToken, ServiceClientOptions options)
        where TClient : class
        where TImplementation : class, TClient
    {
        ArgumentNullException.ThrowIfNull(host, nameof(host));
        ArgumentNullException.ThrowIfNull(authToken, nameof(authToken));

        return services.AddServiceClient<TClient, TImplementation>(
            client =>
            {
                client.BaseAddress = host;
                client.DefaultRequestHeaders.Authorization = authToken;
            },
            options);
    }

    ///<summary>
    /// Registers a service client and its implementation with the specified <paramref name="services"/> collection using the specified <paramref name="client"/> and <paramref name="options"/>.
    /// </summary>
    /// <typeparam name="TClient">The type of service client to register.</typeparam>
    /// <typeparam name="TImplementation">The implementation type of the service client to register.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to register the service client with.</param>
    /// <param name="client">An <see cref="Action{HttpClient}"/> that configures the <see cref="HttpClient"/> used by the service client.</param>
    /// <param name="options">The <see cref="ServiceClientOptions"/> to configure the service client.</param>
    /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/>, <paramref name="client"/>, or <paramref name="options"/> is null.</exception>
    public static IServiceCollection AddServiceClient<TClient, TImplementation>(this IServiceCollection services,
        Action<HttpClient> client, ServiceClientOptions options)
        where TClient : class
        where TImplementation : class, TClient
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        ArgumentNullException.ThrowIfNull(client, nameof(client));

        services.AddHttpClient<TClient, TImplementation>(typeof(TImplementation).Name, client)
            .AddTransientHttpErrorPolicy(policyBuilder =>
                policyBuilder
                    .OrResult(result => result.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                    .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: options.CircuitBreakerPolicy.ExceptionsAllowedBeforeBreaking,
                    durationOfBreak: TimeSpan.FromSeconds(options.CircuitBreakerPolicy.DurationOfBreakSeconds)))
            .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder
                .WaitAndRetryAsync(
                    sleepDurations: Backoff.DecorrelatedJitterBackoffV2(
                        TimeSpan.FromSeconds(options.RetryPolicy.MedianFirstDelayRetrySeconds),
                        options.RetryPolicy.MaxRetryCount)));

        return services;
    }

    /// <summary>
    /// Adds a service client implementation to the service collection with the specified options and default authentication header.
    /// </summary>
    /// <typeparam name="TImplementation">The type of the service client implementation.</typeparam>
    /// <param name="services">The service collection to add the service client to.</param>
    /// <param name="host">The base address of the remote service.</param>
    /// <param name="authToken">The authentication header value to include in requests to the remote service.</param>
    /// <param name="options">The options to configure the service client.</param>
    /// <returns>The service collection with the service client implementation added.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="host"/>, <paramref name="authToken"/>, or <paramref name="options"/> is null.</exception>
    public static IServiceCollection AddServiceClient<TImplementation>(this IServiceCollection services,
        Uri host, AuthenticationHeaderValue authToken, ServiceClientOptions options)
        where TImplementation : class
    {
        ArgumentNullException.ThrowIfNull(host, nameof(host));
        ArgumentNullException.ThrowIfNull(authToken, nameof(authToken));

        return services.AddServiceClient<TImplementation>(
            client =>
            {
                client.BaseAddress = host;
                client.DefaultRequestHeaders.Authorization = authToken;
            },
            options);
    }

    /// <summary>
    /// Adds a service client implementation to the service collection with the specified options and client configuration action.
    /// </summary>
    /// <typeparam name="TImplementation">The type of the service client implementation.</typeparam>
    /// <param name="services">The service collection to add the service client to.</param>
    /// <param name="client">The action to configure the underlying <see cref="HttpClient"/>.</param>
    /// <param name="options">The options to configure the service client.</param>
    /// <returns>The service collection with the service client implementation added.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/>, <paramref name="options"/>, or <paramref name="client"/> is null.</exception>
    public static IServiceCollection AddServiceClient<TImplementation>(this IServiceCollection services,
        Action<HttpClient> client, ServiceClientOptions options)
        where TImplementation : class
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        ArgumentNullException.ThrowIfNull(client, nameof(client));

        services.AddHttpClient<TImplementation>(typeof(TImplementation).Name, client)
            .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder
                    .OrResult(result => result.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                    .CircuitBreakerAsync(
                        handledEventsAllowedBeforeBreaking: options.CircuitBreakerPolicy.ExceptionsAllowedBeforeBreaking,
                        durationOfBreak: TimeSpan.FromSeconds(options.CircuitBreakerPolicy.DurationOfBreakSeconds)))
            .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder
                .WaitAndRetryAsync(
                    sleepDurations: Backoff.DecorrelatedJitterBackoffV2(
                        TimeSpan.FromSeconds(options.RetryPolicy.MedianFirstDelayRetrySeconds),
                        options.RetryPolicy.MaxRetryCount)));

        return services;
    }
}
