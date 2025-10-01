namespace Bioteca.Prism.Domain.Requests.Node;

/// <summary>
/// Request to initiate handshake with a remote node (client role)
/// </summary>
public class InitiateHandshakeRequest
{
    /// <summary>
    /// URL of the remote node to connect to (e.g., https://remote-node.example.com)
    /// </summary>
    public string RemoteNodeUrl { get; set; } = string.Empty;
}
