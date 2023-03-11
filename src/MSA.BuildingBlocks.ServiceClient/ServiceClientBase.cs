using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace MSA.BuildingBlocks.ServiceClient;

/// <summary>
/// Service client which provides a wrapper around the <see cref="HttpClient"/> class to send requests.
/// </summary>
public class ServiceClientBase
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ServiceClientBase> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceClientBase"/> class.
    /// </summary>
    /// <param name="httpClient">An instance of <see cref="HttpClient"/> used to send HTTP requests.</param>
    /// <param name="version">The API version used by the service.</param>
    /// <param name="logger">An instance of <see cref="ILogger{TCategoryName}"/> used for logging.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="httpClient"/>, <paramref name="logger"/>, or <paramref name="httpClient.BaseAddress"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="version"/> is null or empty.</exception>
    public ServiceClientBase(HttpClient httpClient, string version, ILogger<ServiceClientBase> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient, nameof(httpClient));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));
        ArgumentNullException.ThrowIfNull(httpClient.BaseAddress, nameof(httpClient.BaseAddress));
        ArgumentException.ThrowIfNullOrEmpty(version, nameof(version));

        _httpClient = httpClient;
        _logger = logger;
        ServiceUri = new Uri(httpClient.BaseAddress, version);
    }

    protected Uri ServiceUri { get; }

    /// <summary>
    /// Sends an HTTP request and receives a <see cref="ServiceResponse"/>.
    /// </summary>
    /// <exception cref="UnhandledServiceException">Thrown if an error occurs during request processing.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="requestMessage"/> is null.</exception>
    protected async Task<ServiceResponse> SendAsync(HttpRequestMessage requestMessage, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(requestMessage, nameof(requestMessage));

        string requestId = Activity.Current?.RootId ?? Guid.NewGuid().ToString();
        _logger.LogInformation("Internal request '{HttpMethod} {RequestUri}' is starting. RequestId: {RequestId}",
            requestMessage.Method, requestMessage.RequestUri, requestId);

        try
        {
            using HttpResponseMessage responseMessage = await _httpClient
                .SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);

            if (responseMessage.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Internal request '{HttpMethod} {RequestUri}' successful. RequestId: {RequestId}",
                    requestMessage.Method, requestMessage.RequestUri, requestId);

                return new ServiceResponse((int)responseMessage.StatusCode, requestId);
            }

            string content = await responseMessage.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            LogResponse(responseMessage, requestId, content);

            var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(content, Extensions.JsonSerializerOptions);
            return new ServiceResponse((int)responseMessage.StatusCode, errorResponse?.TraceId ?? requestId, errorResponse?.Errors);
        }
        catch (Exception ex)
        {
            throw new UnhandledServiceException(requestId, requestMessage.Method, requestMessage.RequestUri, ex);
        }
    }

    /// <summary>
    /// Sends an HTTP request and receives a <see cref="ServiceResponse{TResponse}"/>.
    /// </summary>
    /// <exception cref="UnhandledServiceException">Thrown if an error occurs during request processing.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="requestMessage"/> is null.</exception>
    protected async Task<ServiceResponse<TResponse>> SendAsync<TResponse>(HttpRequestMessage requestMessage, CancellationToken ct = default)
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(requestMessage, nameof(requestMessage));

        string requestId = Activity.Current?.RootId ?? Guid.NewGuid().ToString();
        _logger.LogInformation("Internal request '{HttpMethod} {RequestUri}' is starting. RequestId: {RequestId}",
            requestMessage.Method, requestMessage.RequestUri, requestId);

        try
        {
            using HttpResponseMessage responseMessage = await _httpClient
                .SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);

            if (responseMessage.IsSuccessStatusCode)
            {
                _logger.LogInformation("Internal request '{HttpMethod} {RequestUri}' successful. RequestId: {RequestId}",
                    requestMessage.Method, requestMessage.RequestUri, requestId);

                var successfulResponse = await responseMessage.Content.ReadFromJsonAsync<TResponse>(Extensions.JsonSerializerOptions, ct);
                return new ServiceResponse<TResponse>(successfulResponse, (int)responseMessage.StatusCode, requestId);
            }

            string failedResponse = await responseMessage.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            LogResponse(responseMessage, requestId, failedResponse);

            var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(failedResponse, Extensions.JsonSerializerOptions);
            return new ServiceResponse<TResponse>(default, (int)responseMessage.StatusCode, errorResponse?.TraceId ?? requestId, errorResponse?.Errors);
        }
        catch (Exception ex)
        {
            throw new UnhandledServiceException(requestId, requestMessage.Method, requestMessage.RequestUri, ex);
        }
    }

    private void LogResponse(HttpResponseMessage responseMessage, string requestId, string content)
    {
        var method = responseMessage.RequestMessage?.Method;
        var requestUri = responseMessage.RequestMessage?.RequestUri;

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogWarning(
                "Internal request '{HttpMethod} {RequestUri}' unsuccessful. StatusCode: {StatusCode}; RequestId: {RequestId}; Content: {Content}",
                method, requestId, responseMessage.StatusCode, requestId, content);
        }
        else
        {
            _logger.LogWarning(
                "Internal request '{HttpMethod} {RequestUri}' unsuccessful. StatusCode: {StatusCode}; RequestId: {RequestId}",
                method, requestUri, responseMessage.StatusCode, requestId);
        }
    }
}