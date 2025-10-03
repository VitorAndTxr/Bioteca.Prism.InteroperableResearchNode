using Bioteca.Prism.Domain.Entities.Node;
using Bioteca.Prism.Domain.Requests.Node;
using Bioteca.Prism.Domain.Responses.Node;

namespace Bioteca.Prism.Core.Middleware.Node;

/// <summary>
/// Service for managing node registry (known/unknown nodes)
/// </summary>
public interface INodeRegistryService
{
    /// <summary>
    /// Check if a node is known and authorized
    /// </summary>
    /// <param name="nodeId">Node identifier</param>
    /// <returns>Node information if found, null otherwise</returns>
    Task<RegisteredNode?> GetNodeAsync(string nodeId);

    /// <summary>
    /// Check if a node is known by certificate fingerprint
    /// </summary>
    /// <param name="certificateFingerprint">SHA-256 fingerprint of certificate</param>
    /// <returns>Node information if found, null otherwise</returns>
    Task<RegisteredNode?> GetNodeByCertificateAsync(string certificateFingerprint);

    /// <summary>
    /// Verify node's certificate signature
    /// </summary>
    /// <param name="request">Identification request with signature</param>
    /// <returns>True if signature is valid</returns>
    Task<bool> VerifyNodeSignatureAsync(NodeIdentifyRequest request);

    /// <summary>
    /// Register a new node (pending approval)
    /// </summary>
    /// <param name="request">Registration request</param>
    /// <returns>Registration response</returns>
    Task<NodeRegistrationResponse> RegisterNodeAsync(NodeRegistrationRequest request);

    /// <summary>
    /// Update node status (approve/revoke)
    /// </summary>
    /// <param name="nodeId">Node identifier</param>
    /// <param name="status">New authorization status</param>
    /// <returns>True if updated successfully</returns>
    Task<bool> UpdateNodeStatusAsync(string nodeId, AuthorizationStatus status);

    /// <summary>
    /// Get all registered nodes
    /// </summary>
    /// <returns>List of all registered nodes</returns>
    Task<List<RegisteredNode>> GetAllNodesAsync();

    /// <summary>
    /// Update last authentication timestamp
    /// </summary>
    /// <param name="nodeId">Node identifier</param>
    /// <returns>True if updated successfully</returns>
    Task<bool> UpdateLastAuthenticationAsync(string nodeId);
}
