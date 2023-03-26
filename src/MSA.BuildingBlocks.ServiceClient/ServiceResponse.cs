using System.Text.Json.Serialization;

namespace MSA.BuildingBlocks.ServiceClient;

/// <summary>
/// Represents a service response with status code and optional errors and trace ID.
/// </summary>
public record ServiceResponse<TResponse>(TResponse Payload, int StatusCode, string TraceId, IReadOnlyCollection<Error> Errors = default) : ServiceResponse(StatusCode, TraceId, Errors);

/// <summary>
/// Gets a value indicating whether the service response represents a successful operation.
/// </summary>
public record ServiceResponse(int StatusCode, string TraceId, IReadOnlyCollection<Error> Errors = default)
{
    public bool IsSuccess => StatusCode is >= 200 and < 300;
}

internal record ErrorResponse
{
    private readonly List<Error> _errors;

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorResponse"/> class.
    /// </summary>
    /// <param name="traceId">The trace ID associated with the error response.</param>
    /// <param name="errors">The collection of errors associated with the error response.</param>
    [JsonConstructor]
    public ErrorResponse(string traceId, IReadOnlyCollection<Error> errors)
    {
        TraceId = traceId;
        _errors = errors?.ToList() ?? new List<Error>();
    }

    /// <summary>
    /// Gets the trace ID associated with the error response.
    /// </summary>
    public string TraceId { get; }

    /// <summary>
    /// Gets the collection of errors associated with the error response.
    /// </summary>
    public IReadOnlyCollection<Error> Errors => _errors.AsReadOnly();
}

/// <summary>
/// Represents an error with a message and a source.
/// </summary>
public record Error
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Error"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="source">The source of the error.</param>
    [JsonConstructor]
    public Error(string message, string source)
    {
        Message = message;
        Source = source;
    }

    /// <summary>
    /// Gets the error message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the source of the error.
    /// </summary>
    public string Source { get; }
}