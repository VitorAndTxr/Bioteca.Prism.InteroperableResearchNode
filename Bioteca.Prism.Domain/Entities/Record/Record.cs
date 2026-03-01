namespace Bioteca.Prism.Domain.Entities.Record;

/// <summary>
/// Represents a data record in a session
/// </summary>
public class Record
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
    /// Collection date
    /// </summary>
    public DateTime CollectionDate { get; set; }

    /// <summary>
    /// Session ID
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Record type
    /// </summary>
    public string RecordType { get; set; } = string.Empty;

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
    public ICollection<RecordChannel> RecordChannels { get; set; } = new List<RecordChannel>();
}
