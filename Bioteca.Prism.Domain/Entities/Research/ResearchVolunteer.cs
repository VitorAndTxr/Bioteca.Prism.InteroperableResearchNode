namespace Bioteca.Prism.Domain.Entities.Research;

/// <summary>
/// Join table for many-to-many relationship between Research and Volunteer
/// </summary>
public class ResearchVolunteer
{
    /// <summary>
    /// Foreign key to research
    /// </summary>
    public Guid ResearchId { get; set; }

    /// <summary>
    /// Foreign key to volunteer
    /// </summary>
    public Guid VolunteerId { get; set; }

    /// <summary>
    /// Enrollment status
    /// </summary>
    public string EnrollmentStatus { get; set; } = string.Empty;

    /// <summary>
    /// Consent date
    /// </summary>
    public DateTime ConsentDate { get; set; }

    /// <summary>
    /// Consent version
    /// </summary>
    public string ConsentVersion { get; set; } = string.Empty;

    /// <summary>
    /// Exclusion reason (if withdrawn)
    /// </summary>
    public string? ExclusionReason { get; set; }

    /// <summary>
    /// Enrollment timestamp
    /// </summary>
    public DateTime EnrolledAt { get; set; }

    /// <summary>
    /// Withdrawal timestamp
    /// </summary>
    public DateTime? WithdrawnAt { get; set; }

    // Navigation properties
    public Research Research { get; set; } = null!;
    public Volunteer.Volunteer Volunteer { get; set; } = null!;
}
