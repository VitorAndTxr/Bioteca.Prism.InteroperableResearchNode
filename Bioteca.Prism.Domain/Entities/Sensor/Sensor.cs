namespace Bioteca.Prism.Domain.Entities.Sensor;

/// <summary>
/// Represents a sensor in a device
/// </summary>
public class Sensor
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid SensorId { get; set; }

    /// <summary>
    /// Foreign key to device
    /// </summary>
    public Guid DeviceId { get; set; }

    /// <summary>
    /// Sensor name
    /// </summary>
    public string SensorName { get; set; } = string.Empty;

    /// <summary>
    /// Maximum sampling rate in Hz
    /// </summary>
    public float MaxSamplingRate { get; set; }

    /// <summary>
    /// Unit of measurement
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// Minimum range
    /// </summary>
    public float MinRange { get; set; }

    /// <summary>
    /// Maximum range
    /// </summary>
    public float MaxRange { get; set; }

    /// <summary>
    /// Accuracy
    /// </summary>
    public float Accuracy { get; set; }

    /// <summary>
    /// Additional information
    /// </summary>
    public string AdditionalInfo { get; set; } = string.Empty;

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public Device.Device Device { get; set; } = null!;
    public ICollection<Record.RecordChannel> RecordChannels { get; set; } = new List<Record.RecordChannel>();
}
