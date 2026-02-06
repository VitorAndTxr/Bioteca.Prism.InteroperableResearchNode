namespace Bioteca.Prism.Domain.Payloads.Volunteer;

/// <summary>
/// Payload for updating an existing volunteer
/// </summary>
public class UpdateVolunteerPayload
{
    /// <summary>
    /// Updated volunteer name (optional)
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Updated volunteer email (optional)
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Updated birth date (optional)
    /// </summary>
    public DateTime? BirthDate { get; set; }

    /// <summary>
    /// Updated gender (optional)
    /// </summary>
    public string? Gender { get; set; }

    /// <summary>
    /// Updated blood type (optional)
    /// </summary>
    public string? BloodType { get; set; }

    /// <summary>
    /// Updated height in meters (optional)
    /// </summary>
    public float? Height { get; set; }

    /// <summary>
    /// Updated weight in kilograms (optional)
    /// </summary>
    public float? Weight { get; set; }

    /// <summary>
    /// Updated medical history (optional)
    /// </summary>
    public string? MedicalHistory { get; set; }

    /// <summary>
    /// Updated consent status (optional)
    /// </summary>
    public string? ConsentStatus { get; set; }
}
