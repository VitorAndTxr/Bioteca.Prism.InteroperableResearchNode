namespace Bioteca.Prism.Domain.DTOs.Volunteer;

/// <summary>
/// Data Transfer Object for Volunteer entity
/// </summary>
public class VolunteerDTO
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid VolunteerId { get; set; }

    /// <summary>
    /// Research node ID this volunteer belongs to
    /// </summary>
    public Guid ResearchNodeId { get; set; }

    /// <summary>
    /// Anonymized volunteer code
    /// </summary>
    public string VolunteerCode { get; set; } = string.Empty;

    /// <summary>
    /// Volunteer full name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Volunteer email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Birth date
    /// </summary>
    public DateTime BirthDate { get; set; }

    /// <summary>
    /// Gender
    /// </summary>
    public string Gender { get; set; } = string.Empty;

    /// <summary>
    /// Blood type
    /// </summary>
    public string BloodType { get; set; } = string.Empty;

    /// <summary>
    /// Height in meters
    /// </summary>
    public float? Height { get; set; }

    /// <summary>
    /// Weight in kilograms
    /// </summary>
    public float? Weight { get; set; }

    /// <summary>
    /// Consent status
    /// </summary>
    public string ConsentStatus { get; set; } = string.Empty;

    /// <summary>
    /// Enrollment timestamp
    /// </summary>
    public DateTime EnrolledAt { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Associated clinical condition SNOMED codes
    /// </summary>
    public List<string> ClinicalConditionCodes { get; set; } = new();

    /// <summary>
    /// Associated clinical event SNOMED codes
    /// </summary>
    public List<string> ClinicalEventCodes { get; set; } = new();

    /// <summary>
    /// Associated medication SNOMED codes
    /// </summary>
    public List<string> MedicationCodes { get; set; } = new();

    /// <summary>
    /// Associated allergy/intolerance SNOMED codes
    /// </summary>
    public List<string> AllergyIntoleranceCodes { get; set; } = new();
}
