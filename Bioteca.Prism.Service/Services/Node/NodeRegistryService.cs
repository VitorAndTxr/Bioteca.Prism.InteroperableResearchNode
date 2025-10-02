using Bioteca.Prism.Domain.Entities.Node;
using Bioteca.Prism.Domain.Requests.Node;
using Bioteca.Prism.Domain.Responses.Node;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Bioteca.Prism.Service.Services.Node;

/// <summary>
/// In-memory implementation of node registry service
/// </summary>
public class NodeRegistryService : INodeRegistryService
{
    private readonly Dictionary<string, RegisteredNode> _nodes = new();
    private readonly Dictionary<string, RegisteredNode> _nodesByCertificate = new();
    private readonly ILogger<NodeRegistryService> _logger;
    private readonly object _lock = new();

    public NodeRegistryService(ILogger<NodeRegistryService> logger)
    {
        _logger = logger;
    }

    public Task<RegisteredNode?> GetNodeAsync(string nodeId)
    {
        lock (_lock)
        {
            _nodes.TryGetValue(nodeId, out var node);
            return Task.FromResult(node);
        }
    }

    public Task<RegisteredNode?> GetNodeByCertificateAsync(string certificateFingerprint)
    {
        lock (_lock)
        {
            _nodesByCertificate.TryGetValue(certificateFingerprint, out var node);
            return Task.FromResult(node);
        }
    }

    public async Task<bool> VerifyNodeSignatureAsync(NodeIdentifyRequest request)
    {
        try
        {
            // Load certificate from base64
            var certBytes = Convert.FromBase64String(request.Certificate);
            using var cert = new X509Certificate2(certBytes);

            // Get public key
            using var rsa = cert.GetRSAPublicKey();
            if (rsa == null)
            {
                _logger.LogWarning("Certificate does not contain RSA public key");
                return false;
            }

            // Build the signed data: ChannelId + NodeId + Timestamp
            var signedData = $"{request.ChannelId}{request.NodeId}{request.Timestamp:O}";
            var dataBytes = Encoding.UTF8.GetBytes(signedData);

            // Verify signature
            var signatureBytes = Convert.FromBase64String(request.Signature);
            var isValid = rsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            if (isValid)
            {
                _logger.LogInformation("Node {NodeId} signature verified successfully", request.NodeId);
            }
            else
            {
                _logger.LogWarning("Invalid signature from node {NodeId}", request.NodeId);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying node signature for {NodeId}", request.NodeId);
            return false;
        }
    }

    public Task<NodeRegistrationResponse> RegisterNodeAsync(NodeRegistrationRequest request)
    {
        lock (_lock)
        {
            try
            {
                // Check if node already exists
                if (_nodes.ContainsKey(request.NodeId))
                {
                return Task.FromResult(new NodeRegistrationResponse
                {
                    Success = false,
                    Status = AuthorizationStatus.Revoked,
                    Message = "Node ID already registered"
                });
                }

                // Calculate certificate fingerprint
                var certBytes = Convert.FromBase64String(request.Certificate);
                var fingerprint = CalculateCertificateFingerprint(certBytes);

                // Check if certificate is already registered
                if (_nodesByCertificate.ContainsKey(fingerprint))
                {
                return Task.FromResult(new NodeRegistrationResponse
                {
                    Success = false,
                    Status = AuthorizationStatus.Revoked,
                    Message = "Certificate already registered with another node"
                });
                }

                // Create new registered node (pending approval)
                var registeredNode = new RegisteredNode
                {
                    NodeId = request.NodeId,
                    NodeName = request.NodeName,
                    Certificate = request.Certificate,
                    CertificateFingerprint = fingerprint,
                    NodeUrl = request.NodeUrl,
                    ContactInfo = request.ContactInfo,
                    InstitutionDetails = request.InstitutionDetails,
                    Status = AuthorizationStatus.Pending,
                    Capabilities = request.RequestedCapabilities,
                    RegisteredAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _nodes[request.NodeId] = registeredNode;
                _nodesByCertificate[fingerprint] = registeredNode;

                var registrationId = Guid.NewGuid().ToString();

                _logger.LogInformation("Node {NodeId} registered successfully (pending approval)", request.NodeId);

                return Task.FromResult(new NodeRegistrationResponse
                {
                    Success = true,
                    RegistrationId = registrationId,
                    Status = AuthorizationStatus.Pending,
                    Message = "Registration received. Pending administrator approval.",
                    EstimatedApprovalTime = TimeSpan.FromHours(24)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering node {NodeId}", request.NodeId);
                return Task.FromResult(new NodeRegistrationResponse
                {
                    Success = false,
                    Status = AuthorizationStatus.Revoked,
                    Message = $"Registration failed: {ex.Message}"
                });
            }
        }
    }

    public Task<bool> UpdateNodeStatusAsync(string nodeId, AuthorizationStatus status)
    {
        lock (_lock)
        {
            if (!_nodes.TryGetValue(nodeId, out var node))
            {
                return Task.FromResult(false);
            }

            node.Status = status;
            node.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation("Node {NodeId} status updated to {Status}", nodeId, status);

            return Task.FromResult(true);
        }
    }

    public Task<List<RegisteredNode>> GetAllNodesAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(_nodes.Values.ToList());
        }
    }

    public Task<bool> UpdateLastAuthenticationAsync(string nodeId)
    {
        lock (_lock)
        {
            if (!_nodes.TryGetValue(nodeId, out var node))
            {
                return Task.FromResult(false);
            }

            node.LastAuthenticatedAt = DateTime.UtcNow;

            _logger.LogInformation("Node {NodeId} last authentication time updated", nodeId);

            return Task.FromResult(true);
        }
    }

    private static string CalculateCertificateFingerprint(byte[] certificateBytes)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(certificateBytes);
        return Convert.ToBase64String(hash);
    }
}
