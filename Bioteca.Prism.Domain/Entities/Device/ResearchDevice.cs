namespace Bioteca.Prism.Domain.Entities.Device;

/// <summary>
/// Join entity for many-to-many relationship between Research and Device
/// </summary>
public class ResearchDevice
{
    /// <summary>
    /// Foreign key to research
    /// </summary>
    public Guid ResearchId { get; set; }

    /// <summary>
    /// Foreign key to device
    /// </summary>
    public Guid DeviceId { get; set; }

    /// <summary>
    /// Device role or purpose in the research
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// When the device was added to the research
    /// </summary>
    public DateTime AddedAt { get; set; }

    /// <summary>
    /// When the device was removed from the research (null if still active)
    /// </summary>
    public DateTime? RemovedAt { get; set; }

    /// <summary>
    /// Calibration status
    /// </summary>
    public string CalibrationStatus { get; set; } = string.Empty;

    /// <summary>
    /// Last calibration date
    /// </summary>
    public DateTime? LastCalibrationDate { get; set; }

    // Navigation properties
    public Research.Research Research { get; set; } = null!;
    public Device Device { get; set; } = null!;
}
