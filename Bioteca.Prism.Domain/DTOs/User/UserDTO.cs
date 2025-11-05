namespace Bioteca.Prism.Domain.DTOs.User;

/// <summary>
/// Data Transfer Object for User entity - excludes sensitive fields like PasswordHash
/// </summary>
public class UserDTO
{
    /// <summary>
    /// Unique identifier for the user
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User login/username
    /// </summary>
    public string Login { get; set; } = string.Empty;

    /// <summary>
    /// User role (e.g., "Admin", "Researcher", "Volunteer")
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the user was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Timestamp when the user was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Associated researcher (if applicable)
    /// </summary>
    public ResearcherInfoDto? Researcher { get; set; }
}

public class ResearcherInfoDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Orcid { get; set; } = string.Empty;
}

public class ResearcherDTO
{
    public Guid ResearcherId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Orcid { get; set; } = string.Empty;
}
