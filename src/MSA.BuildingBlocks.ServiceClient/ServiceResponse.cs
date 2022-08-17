namespace MSA.BuildingBlocks.ServiceClient;

public class ServiceResponse<TResponse> : ServiceResponse
{
    public ServiceResponse(TResponse data, int statusCode, ErrorResponse validationErrors = null)
        : base(statusCode, validationErrors)
    {
        Data = data;
    }

    public TResponse Data { get; }
}

public class ServiceResponse
{
    public ServiceResponse(int statusCode)
    {
        StatusCode = statusCode;
    }

    public ServiceResponse(int statusCode, ErrorResponse validationErrors)
        : this(statusCode)
    {
        ValidationErrors = validationErrors;
    }

    public int StatusCode { get; }
    public ErrorResponse ValidationErrors { get; }
    public bool IsSuccess => StatusCode is >= 200 and < 300;
}

public class ErrorResponse
{
    private readonly List<Error> _errors;

    public ErrorResponse(string traceId, IEnumerable<Error> errors)
    {
        TraceId = traceId;
        _errors = errors.ToList();
    }

    public string TraceId { get; }
    public IReadOnlyCollection<Error> Errors => _errors.AsReadOnly();
}

public record Error(string Message, string Source);