namespace Bioteca.Prism.Domain.Entities.Volunteer;

/// <summary>
/// Volunteer-specific allergy/intolerance instance
/// </summary>
public class VolunteerAllergyIntolerance
{
    /// <summary>
    /// Unique identifier (primary key)
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Volunteer ID (foreign key)
    /// </summary>
    public Guid VolunteerId { get; set; }

    /// <summary>
    /// Allergy/intolerance SNOMED code (foreign key)
    /// </summary>
    public string AllergyIntoleranceSnomedCode { get; set; } = string.Empty;

    /// <summary>
    /// Criticality (low, high, unable-to-assess)
    /// </summary>
    public string Criticality { get; set; } = string.Empty;

    /// <summary>
    /// Clinical status (active, inactive, resolved)
    /// </summary>
    public string ClinicalStatus { get; set; } = string.Empty;

    /// <summary>
    /// JSON array of manifestations/symptoms
    /// </summary>
    public string Manifestations { get; set; } = string.Empty;

    /// <summary>
    /// Date when the allergy/intolerance first occurred
    /// </summary>
    public DateTime? OnsetDate { get; set; }

    /// <summary>
    /// Date of last known occurrence
    /// </summary>
    public DateTime? LastOccurrence { get; set; }

    /// <summary>
    /// Verification status (confirmed, unconfirmed, refuted)
    /// </summary>
    public string VerificationStatus { get; set; } = string.Empty;

    /// <summary>
    /// Researcher who recorded this allergy/intolerance
    /// </summary>
    public Guid RecordedBy { get; set; }

    /// <summary>
    /// When this allergy/intolerance record was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this allergy/intolerance record was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    /// <summary>
    /// Volunteer with this allergy/intolerance
    /// </summary>
    public Volunteer Volunteer { get; set; } = null!;

    /// <summary>
    /// Allergy/intolerance catalog entry
    /// </summary>
    public Clinical.AllergyIntolerance AllergyIntolerance { get; set; } = null!;

    /// <summary>
    /// Researcher who documented this allergy/intolerance
    /// </summary>
    public Researcher.Researcher Recorder { get; set; } = null!;
}
