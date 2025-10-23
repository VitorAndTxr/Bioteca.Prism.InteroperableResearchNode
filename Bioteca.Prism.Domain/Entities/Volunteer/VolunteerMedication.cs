namespace Bioteca.Prism.Domain.Entities.Volunteer;

/// <summary>
/// Volunteer medication prescription/administration record
/// </summary>
public class VolunteerMedication
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
    /// Medication SNOMED code (foreign key)
    /// </summary>
    public string MedicationSnomedCode { get; set; } = string.Empty;

    /// <summary>
    /// Clinical condition ID being treated (foreign key, optional)
    /// </summary>
    public Guid? ConditionId { get; set; }

    /// <summary>
    /// Dosage information (e.g., "500mg", "1 tablet")
    /// </summary>
    public string Dosage { get; set; } = string.Empty;

    /// <summary>
    /// Frequency of administration (e.g., "twice daily", "every 8 hours")
    /// </summary>
    public string Frequency { get; set; } = string.Empty;

    /// <summary>
    /// Route of administration (e.g., "oral", "intravenous", "topical")
    /// </summary>
    public string Route { get; set; } = string.Empty;

    /// <summary>
    /// Date when medication started
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Date when medication ended (null if ongoing)
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Current status (active, completed, stopped, on-hold)
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Additional notes about the medication
    /// </summary>
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// Researcher who prescribed/recorded this medication
    /// </summary>
    public Guid RecordedBy { get; set; }

    /// <summary>
    /// When this medication record was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this medication record was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    /// <summary>
    /// Volunteer taking this medication
    /// </summary>
    public Volunteer Volunteer { get; set; } = null!;

    /// <summary>
    /// Medication catalog entry
    /// </summary>
    public Clinical.Medication Medication { get; set; } = null!;

    /// <summary>
    /// Clinical condition being treated (optional)
    /// </summary>
    public VolunteerClinicalCondition? Condition { get; set; }
}
