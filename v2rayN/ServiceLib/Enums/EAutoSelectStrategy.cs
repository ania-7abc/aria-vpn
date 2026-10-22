namespace ServiceLib.Enums;

/// <summary>
/// Strategy used by the automatic server selection.
/// </summary>
public enum EAutoSelectStrategy
{
    /// <summary>
    /// Pick the server with the lowest latency.
    /// </summary>
    LeastPing,

    /// <summary>
    /// Pick the server with the highest download speed.
    /// </summary>
    Fastest,

    /// <summary>
    /// Pick a random reachable server (helps to spread the load).
    /// </summary>
    Random,

    /// <summary>
    /// Keep the current server as long as it is reachable, otherwise switch to the first working one.
    /// </summary>
    Fallback
}
