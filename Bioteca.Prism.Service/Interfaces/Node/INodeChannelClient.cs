using Bioteca.Prism.Domain.Errors.Node;
using Bioteca.Prism.Domain.Requests.Node;
using Bioteca.Prism.Domain.Responses.Node;

namespace Bioteca.Prism.Service.Interfaces.Node;

/// <summary>
/// Client service for initiating channel handshake with remote nodes
/// </summary>
public interface INodeChannelClient
{
    /// <summary>
    /// Initiate Phase 1 handshake with a remote node
    /// </summary>
    /// <param name="remoteNodeUrl">URL of the remote node (e.g., https://remote-node.com)</param>
    /// <returns>Channel establishment result with channel ID and symmetric key</returns>
    Task<ChannelEstablishmentResult> OpenChannelAsync(string remoteNodeUrl);

    /// <summary>
    /// Close an established channel
    /// </summary>
    /// <param name="channelId">Channel identifier</param>
    Task CloseChannelAsync(string channelId);

    /// <summary>
    /// Phase 2: Identify node over encrypted channel
    /// </summary>
    /// <param name="channelId">Channel identifier</param>
    /// <param name="request">Node identification request</param>
    /// <returns>Node status response</returns>
    Task<NodeStatusResponse> IdentifyNodeAsync(string channelId, NodeIdentifyRequest request);

    /// <summary>
    /// Phase 2: Register node over encrypted channel
    /// </summary>
    /// <param name="channelId">Channel identifier</param>
    /// <param name="request">Node registration request</param>
    /// <returns>Registration response</returns>
    Task<NodeRegistrationResponse> RegisterNodeAsync(string channelId, NodeRegistrationRequest request);
}

/// <summary>
/// Result of channel establishment
/// </summary>
public class ChannelEstablishmentResult
{
    /// <summary>
    /// Whether channel establishment was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Channel identifier (from X-Channel-Id header)
    /// </summary>
    public string? ChannelId { get; set; }

    /// <summary>
    /// Derived symmetric key for encryption
    /// </summary>
    public byte[]? SymmetricKey { get; set; }

    /// <summary>
    /// Selected cipher algorithm
    /// </summary>
    public string? SelectedCipher { get; set; }

    /// <summary>
    /// Remote node URL
    /// </summary>
    public string? RemoteNodeUrl { get; set; }

    /// <summary>
    /// Error details if establishment failed
    /// </summary>
    public HandshakeError? Error { get; set; }

    /// <summary>
    /// Client and server nonces for key derivation
    /// </summary>
    public string? ClientNonce { get; set; }
    public string? ServerNonce { get; set; }
}

/// <summary>
/// Client-side channel context
/// </summary>
public class ClientChannelContext
{
    public string ChannelId { get; set; } = string.Empty;
    public byte[] SymmetricKey { get; set; } = Array.Empty<byte>();
    public string SelectedCipher { get; set; } = string.Empty;
    public string RemoteNodeUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
