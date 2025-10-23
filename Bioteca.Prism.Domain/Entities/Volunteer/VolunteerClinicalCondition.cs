namespace Bioteca.Prism.Domain.Entities.Volunteer;

/// <summary>
/// Volunteer clinical condition diagnosis
/// </summary>
public class VolunteerClinicalCondition
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
    /// Clinical condition SNOMED code (foreign key)
    /// </summary>
    public string SnomedCode { get; set; } = string.Empty;

    /// <summary>
    /// Clinical status (active, recurrence, relapse, inactive, remission, resolved)
    /// </summary>
    public string ClinicalStatus { get; set; } = string.Empty;

    /// <summary>
    /// Date when the condition started
    /// </summary>
    public DateTime? OnsetDate { get; set; }

    /// <summary>
    /// Date when the condition was resolved/ended
    /// </summary>
    public DateTime? AbatementDate { get; set; }

    /// <summary>
    /// Severity code (foreign key, optional)
    /// </summary>
    public string? SeverityCode { get; set; }

    /// <summary>
    /// Verification status (unconfirmed, provisional, differential, confirmed, refuted)
    /// </summary>
    public string VerificationStatus { get; set; } = string.Empty;

    /// <summary>
    /// Clinical notes about the condition
    /// </summary>
    public string ClinicalNotes { get; set; } = string.Empty;

    /// <summary>
    /// Researcher who recorded this condition
    /// </summary>
    public Guid RecordedBy { get; set; }

    /// <summary>
    /// When this condition record was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this condition record was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    /// <summary>
    /// Volunteer diagnosed with this condition
    /// </summary>
    public Volunteer Volunteer { get; set; } = null!;

    /// <summary>
    /// Clinical condition catalog entry
    /// </summary>
    public Clinical.ClinicalCondition ClinicalCondition { get; set; } = null!;

    /// <summary>
    /// Severity classification (optional)
    /// </summary>
    public Snomed.SnomedSeverityCode? Severity { get; set; }

    /// <summary>
    /// Medications prescribed to treat this condition
    /// </summary>
    public ICollection<VolunteerMedication> Medications { get; set; } = new List<VolunteerMedication>();
}
