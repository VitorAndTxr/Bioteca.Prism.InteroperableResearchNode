using Bioteca.Prism.Domain.Entities.Node;
using Bioteca.Prism.Domain.Responses.Node;

namespace Bioteca.Prism.Data.Interfaces.Node;

/// <summary>
/// Repository interface for node persistence operations
/// </summary>
public interface INodeRepository
{
    /// <summary>
    /// Get a node by its Guid ID
    /// </summary>
    Task<ResearchNode?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a node by certificate fingerprint
    /// </summary>
    Task<ResearchNode?> GetByCertificateFingerprintAsync(string fingerprint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all research nodes
    /// </summary>
    Task<List<ResearchNode>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get nodes by status
    /// </summary>
    Task<List<ResearchNode>> GetByStatusAsync(AuthorizationStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new node
    /// </summary>
    Task<ResearchNode> AddAsync(ResearchNode node, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing node
    /// </summary>
    Task<ResearchNode> UpdateAsync(ResearchNode node, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a node by Guid ID
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a node exists by Guid ID
    /// </summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a certificate fingerprint is already registered
    /// </summary>
    Task<bool> CertificateExistsAsync(string fingerprint, CancellationToken cancellationToken = default);
}
