namespace MSA.BuildingBlocks.ServiceClient;

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