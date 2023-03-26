namespace MSA.BuildingBlocks.ServiceClient;

/// <summary>
///     Standard service client policies.
/// </summary>
public class ServiceClientOptions
{
    /// <inheritdoc cref="ClientRetryPolicy"/>
    public ClientRetryPolicy RetryPolicy { get; set; } = new();

    /// <inheritdoc cref="ClientCircuitBreakerPolicy"/>
    public ClientCircuitBreakerPolicy CircuitBreakerPolicy { get; set; } = new();
}
