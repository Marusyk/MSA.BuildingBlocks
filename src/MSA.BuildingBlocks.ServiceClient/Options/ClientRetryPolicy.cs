namespace MSA.BuildingBlocks.ServiceClient;

/// <summary>
///     Retry policy settings for service client.
/// </summary>
public sealed class ClientRetryPolicy
{
    /// <summary>
    ///     Default value 3.
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    ///     Default value 1 second. 
    /// </summary>
    public int MedianFirstDelayRetrySeconds { get; set; } = 1;
}
