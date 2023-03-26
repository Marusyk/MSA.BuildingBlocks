namespace MSA.BuildingBlocks.ServiceClient;

/// <summary>
/// Represents an unhandled exception that occurred while making a service request.
/// </summary>
public sealed class UnhandledServiceException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnhandledServiceException"/> class with the specified request ID, HTTP method, request URI, and inner exception.
    /// </summary>
    /// <param name="requestId">The ID of the request that caused the exception.</param>
    /// <param name="httpMethod">The HTTP method used for the request that caused the exception.</param>
    /// <param name="requestUri">The URI of the request that caused the exception.</param>
    /// <param name="innerException">The exception that caused the failure.</param>
    public UnhandledServiceException(string requestId, HttpMethod httpMethod, Uri requestUri, Exception innerException)
        : base(
            $"Unhandled service exception {httpMethod} {requestUri}; Message: {innerException.Message}; RequestId: {requestId}.",
            innerException)
    {
        RequestId = requestId;
        DateTime = DateTime.UtcNow;
        HttpMethod = httpMethod;
        RequestUri = requestUri;
    }

    /// <summary>
    /// Gets the ID of the request that caused the exception.
    /// </summary>
    public string RequestId { get; }

    /// <summary>
    /// Gets the date and time when the exception occurred, in UTC.
    /// </summary>
    public DateTime DateTime { get; }

    /// <summary>
    /// Gets the HTTP method used for the request that caused the exception.
    /// </summary>
    public HttpMethod HttpMethod { get; }

    /// <summary>
    /// Gets the URI of the request that caused the exception.
    /// </summary>
    public Uri RequestUri { get; }
}