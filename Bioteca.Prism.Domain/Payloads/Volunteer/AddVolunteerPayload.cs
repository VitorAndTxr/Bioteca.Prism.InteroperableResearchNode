namespace Bioteca.Prism.Domain.Payloads.Volunteer;

/// <summary>
/// Payload for creating a new volunteer
/// </summary>
public class AddVolunteerPayload
{
    /// <summary>
    /// Volunteer full name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Volunteer email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Anonymized volunteer code (hash of sanitized CPF)
    /// </summary>
    public string VolunteerCode { get; set; } = string.Empty;

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
    /// Consent status (e.g., Pending, Consented, Withdrawn)
    /// </summary>
    public string ConsentStatus { get; set; } = string.Empty;

    /// <summary>
    /// Research node ID this volunteer belongs to
    /// </summary>
    public Guid ResearchNodeId { get; set; }

    /// <summary>
    /// SNOMED codes for clinical conditions to associate on creation (null = skip)
    /// </summary>
    public List<string>? ClinicalConditionCodes { get; set; }

    /// <summary>
    /// SNOMED codes for clinical events to associate on creation (null = skip)
    /// </summary>
    public List<string>? ClinicalEventCodes { get; set; }

    /// <summary>
    /// SNOMED codes for medications to associate on creation (null = skip)
    /// </summary>
    public List<string>? MedicationCodes { get; set; }

    /// <summary>
    /// SNOMED codes for allergy/intolerances to associate on creation (null = skip)
    /// </summary>
    public List<string>? AllergyIntoleranceCodes { get; set; }
}
