namespace Bioteca.Prism.Domain.Entities.Record;

/// <summary>
/// Represents an annotation attached to a recording session
/// </summary>
public class SessionAnnotation
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to record session
    /// </summary>
    public Guid RecordSessionId { get; set; }

    /// <summary>
    /// Annotation text content
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public RecordSession RecordSession { get; set; } = null!;
}
