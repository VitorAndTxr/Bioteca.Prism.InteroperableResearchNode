namespace Bioteca.Prism.Domain.Payloads.User;

/// <summary>
/// Payload for updating an existing user
/// </summary>
public class UpdateUserPayload
{
    /// <summary>
    /// Updated login/username (optional)
    /// </summary>
    public string? Login { get; set; }

    /// <summary>
    /// Updated role (optional)
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// Associated researcher ID (optional, can be null to remove association)
    /// </summary>
    public Guid? ResearcherId { get; set; }
}
