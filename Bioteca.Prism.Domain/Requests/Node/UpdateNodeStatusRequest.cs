using Bioteca.Prism.Domain.Responses.Node;

namespace Bioteca.Prism.Domain.Requests.Node;

/// <summary>
/// Request to update a node's authorization status
/// </summary>
public class UpdateNodeStatusRequest
{
    /// <summary>
    /// New authorization status
    /// </summary>
    public AuthorizationStatus Status { get; set; }
}
