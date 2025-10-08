namespace Bioteca.Prism.Domain.Entities.Device;

/// <summary>
/// Represents a device used in research
/// </summary>
public class Device
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid DeviceId { get; set; }

    /// <summary>
    /// Device name
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// Manufacturer
    /// </summary>
    public string Manufacturer { get; set; } = string.Empty;

    /// <summary>
    /// Model
    /// </summary>
    public string Model { get; set; } = string.Empty;

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
    /// <summary>
    /// Research projects using this device (many-to-many)
    /// </summary>
    public ICollection<ResearchDevice> ResearchDevices { get; set; } = new List<ResearchDevice>();

    /// <summary>
    /// Sensors attached to this device
    /// </summary>
    public ICollection<Sensor.Sensor> Sensors { get; set; } = new List<Sensor.Sensor>();
}
