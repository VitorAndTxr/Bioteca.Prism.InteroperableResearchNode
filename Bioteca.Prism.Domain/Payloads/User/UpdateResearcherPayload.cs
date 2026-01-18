namespace Bioteca.Prism.Domain.Payloads.User;

/// <summary>
/// Payload for updating an existing researcher
/// </summary>
public class UpdateResearcherPayload
{
    /// <summary>
    /// Updated researcher name (optional)
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Updated email address (optional)
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Updated institution affiliation (optional)
    /// </summary>
    public string? Institution { get; set; }

    /// <summary>
    /// Updated role/position (optional)
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// Updated ORCID identifier (optional)
    /// </summary>
    public string? Orcid { get; set; }
}
