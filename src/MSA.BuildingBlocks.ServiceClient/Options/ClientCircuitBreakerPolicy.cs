namespace MSA.BuildingBlocks.ServiceClient;

/// <summary>
///     Circuit breaker policy settings for service client.
/// </summary>
public sealed class ClientCircuitBreakerPolicy
{
    /// <summary>
    ///     Default value 3.
    /// </summary>
    public int ExceptionsAllowedBeforeBreaking { get; set; } = 1;

    /// <summary>
    ///     Default value 3 minutes (180 seconds).
    /// </summary>
    public int DurationOfBreakSeconds { get; set; } = 180; 
}
