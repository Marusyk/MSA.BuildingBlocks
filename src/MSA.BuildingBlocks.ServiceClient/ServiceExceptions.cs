using System.Net;

namespace MSA.BuildingBlocks.ServiceClient;

public sealed class InternalServiceException : Exception
{
    public InternalServiceException(string requestId, HttpMethod httpMethod, Uri requestUri, HttpStatusCode statusCode, string content)
        : base($"Internal service exception {statusCode} {httpMethod} {requestUri}; RequestId: {requestId}.")
    {
        RequestId = requestId;
        DateTime = DateTime.UtcNow;
        HttpMethod = httpMethod;
        RequestUri = requestUri;
        StatusCode = (int)statusCode;
        Content = content;
    }

    public string RequestId { get; }
    public DateTime DateTime { get; }
    public HttpMethod HttpMethod { get; }
    public Uri RequestUri { get; }
    public int StatusCode { get; }
    public string Content { get; }
}

public sealed class UnhandledServiceException : Exception
{
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

    public string RequestId { get; }
    public DateTime DateTime { get; }
    public HttpMethod HttpMethod { get; }
    public Uri RequestUri { get; }
}