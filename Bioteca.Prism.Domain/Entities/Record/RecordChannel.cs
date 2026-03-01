namespace Bioteca.Prism.Domain.Entities.Record;

/// <summary>
/// Represents a channel in a record
/// </summary>
public class RecordChannel
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to record
    /// </summary>
    public Guid RecordId { get; set; }

    /// <summary>
    /// Foreign key to sensor
    /// </summary>
    public Guid? SensorId { get; set; }

    /// <summary>
    /// Signal type
    /// </summary>
    public string SignalType { get; set; } = string.Empty;

    /// <summary>
    /// File URL where data is stored
    /// </summary>
    public string FileUrl { get; set; } = string.Empty;

    /// <summary>
    /// Sampling rate in Hz
    /// </summary>
    public float SamplingRate { get; set; }

    /// <summary>
    /// Number of samples
    /// </summary>
    public int SamplesCount { get; set; }

    /// <summary>
    /// Start timestamp
    /// </summary>
    public DateTime StartTimestamp { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp (used as watermark for incremental sync)
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public Record Record { get; set; } = null!;
    public Sensor.Sensor? Sensor { get; set; }
}
