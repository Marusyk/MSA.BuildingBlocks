using System.Text.Json.Serialization;

namespace MSA.BuildingBlocks.ServiceClient;

public record ServiceResponse<TResponse>(TResponse Payload, int StatusCode, string TraceId, IReadOnlyCollection<Error> Errors = default) : ServiceResponse(StatusCode, TraceId, Errors);

public record ServiceResponse(int StatusCode, string TraceId, IReadOnlyCollection<Error> Errors = default)
{
    public bool IsSuccess => StatusCode is >= 200 and < 300;
}

internal record ErrorResponse
{
    private readonly List<Error> _errors;

    [JsonConstructor]
    public ErrorResponse(string traceId, IReadOnlyCollection<Error> errors)
    {
        TraceId = traceId;
        _errors = errors?.ToList() ?? new List<Error>();
    }

    public string TraceId { get; }
    public IReadOnlyCollection<Error> Errors => _errors.AsReadOnly();
}

public record Error
{
    [JsonConstructor]
    public Error(string message, string source)
    {
        Message = message;
        Source = source;
    }

    public string Message { get; }
    public string Source { get; }
}