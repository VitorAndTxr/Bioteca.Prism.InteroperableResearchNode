namespace Bioteca.Prism.Domain.Errors.Node;

/// <summary>
/// Error response for handshake failures
/// </summary>
public class Error
{
    /// <summary>
    /// Error details
    /// </summary>
    public ErrorDetails ErrorDetail { get; set; } = new();
}

/// <summary>
/// Detailed error information
/// </summary>
public class ErrorDetails
{
    /// <summary>
    /// Error code (e.g., "ERR_INVALID_EPHEMERAL_KEY")
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable error message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Additional error details (optional)
    /// </summary>
    public Dictionary<string, object>? Details { get; set; }

    /// <summary>
    /// Whether this error is retryable
    /// </summary>
    public bool Retryable { get; set; }

    /// <summary>
    /// When to retry (optional, e.g., "after_registration")
    /// </summary>
    public string? RetryAfter { get; set; }
}
