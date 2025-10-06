using Bioteca.Prism.Core.Middleware.Node;
using Bioteca.Prism.Data.Repositories.Node;
using Bioteca.Prism.Domain.Entities.Node;
using Bioteca.Prism.Domain.Requests.Node;
using Bioteca.Prism.Domain.Responses.Node;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Bioteca.Prism.Service.Services.Node;

/// <summary>
/// PostgreSQL-backed implementation of node registry service using repository pattern
/// </summary>
public class PostgreSqlNodeRegistryService : INodeRegistryService
{
    private readonly INodeRepository _repository;
    private readonly ILogger<PostgreSqlNodeRegistryService> _logger;

    public PostgreSqlNodeRegistryService(INodeRepository repository, ILogger<PostgreSqlNodeRegistryService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ResearchNode?> GetNodeAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<ResearchNode?> GetNodeByCertificateAsync(string certificateFingerprint)
    {
        return await _repository.GetByCertificateFingerprintAsync(certificateFingerprint);
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

    public async Task<NodeRegistrationResponse> RegisterNodeAsync(NodeRegistrationRequest request)
    {
        try
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.NodeId))
            {
                return new NodeRegistrationResponse
                {
                    Success = false,
                    Status = AuthorizationStatus.Revoked,
                    Message = "NodeId is required"
                };
            }

            if (string.IsNullOrWhiteSpace(request.NodeName))
            {
                return new NodeRegistrationResponse
                {
                    Success = false,
                    Status = AuthorizationStatus.Revoked,
                    Message = "NodeName is required"
                };
            }

            // Validate and parse certificate
            byte[] certBytes;
            try
            {
                certBytes = Convert.FromBase64String(request.Certificate);
            }
            catch (FormatException)
            {
                return new NodeRegistrationResponse
                {
                    Success = false,
                    Status = AuthorizationStatus.Revoked,
                    Message = "Certificate must be a valid Base64 string"
                };
            }

            // Try to load certificate to validate format
            X509Certificate2? cert = null;
            try
            {
                cert = new X509Certificate2(certBytes);

                // Check if certificate is expired
                if (cert.NotAfter < DateTime.Now)
                {
                    return new NodeRegistrationResponse
                    {
                        Success = false,
                        Status = AuthorizationStatus.Revoked,
                        Message = "Certificate has expired"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Invalid certificate format for node {NodeId}", request.NodeId);
                return new NodeRegistrationResponse
                {
                    Success = false,
                    Status = AuthorizationStatus.Revoked,
                    Message = "Invalid certificate format"
                };
            }
            finally
            {
                cert?.Dispose();
            }

            // Calculate certificate fingerprint
            var fingerprint = CalculateCertificateFingerprint(certBytes);

            // Check if certificate is already registered (use certificate fingerprint as natural key)
            var existingNode = await _repository.GetByCertificateFingerprintAsync(fingerprint);
            if (existingNode != null)
            {
                // Update existing node information (certificate fingerprint stays the same)
                // This is a re-registration with the same certificate

                // If requesting higher access level, require re-approval
                if (existingNode.NodeAccessLevel < request.RequestedNodeAccessLevel)
                {
                    existingNode.Status = AuthorizationStatus.Pending;
                }

                existingNode.NodeName = request.NodeName;
                existingNode.Certificate = request.Certificate;
                existingNode.CertificateFingerprint = fingerprint;
                existingNode.NodeUrl = request.NodeUrl;
                existingNode.ContactInfo = request.ContactInfo;
                existingNode.InstitutionDetails = request.InstitutionDetails;
                existingNode.NodeAccessLevel = request.RequestedNodeAccessLevel;
                existingNode.UpdatedAt = DateTime.UtcNow;

                await _repository.UpdateAsync(existingNode);

                _logger.LogInformation("Node {NodeId} information updated successfully", request.NodeId);

                return new NodeRegistrationResponse
                {
                    Success = true,
                    RegistrationId = existingNode.Id.ToString(),
                    Status = existingNode.Status,
                    Message = "Node information updated successfully.",
                    EstimatedApprovalTime = existingNode.Status == AuthorizationStatus.Pending ? TimeSpan.FromHours(24) : null
                };
            }

            // Create new registered node (pending approval)
            var registeredNode = new ResearchNode
            {
                Id = Guid.NewGuid(),
                NodeName = request.NodeName,
                Certificate = request.Certificate,
                CertificateFingerprint = fingerprint,
                NodeUrl = request.NodeUrl,
                ContactInfo = request.ContactInfo,
                InstitutionDetails = request.InstitutionDetails,
                Status = AuthorizationStatus.Pending,
                NodeAccessLevel = request.RequestedNodeAccessLevel,
                RegisteredAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(registeredNode);

            _logger.LogInformation("Node {NodeName} registered successfully with Id {Id} (pending approval)", request.NodeName, registeredNode.Id);

            return new NodeRegistrationResponse
            {
                Success = true,
                RegistrationId = registeredNode.Id.ToString(),
                Status = AuthorizationStatus.Pending,
                Message = "Registration received. Pending administrator approval.",
                EstimatedApprovalTime = TimeSpan.FromHours(24)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering node {NodeId}", request.NodeId);
            return new NodeRegistrationResponse
            {
                Success = false,
                Status = AuthorizationStatus.Revoked,
                Message = $"Registration failed: {ex.Message}"
            };
        }
    }

    public async Task<bool> UpdateNodeStatusAsync(Guid id, AuthorizationStatus status)
    {
        var node = await _repository.GetByIdAsync(id);
        if (node == null)
        {
            return false;
        }

        node.Status = status;
        node.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(node);

        _logger.LogInformation("Node {Id} status updated to {Status}", id, status);

        return true;
    }

    public async Task<List<ResearchNode>> GetAllNodesAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<bool> UpdateLastAuthenticationAsync(Guid id)
    {
        var node = await _repository.GetByIdAsync(id);
        if (node == null)
        {
            return false;
        }

        node.LastAuthenticatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(node);

        _logger.LogInformation("Node {Id} last authentication time updated", id);

        return true;
    }

    private static string CalculateCertificateFingerprint(byte[] certificateBytes)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(certificateBytes);
        return Convert.ToBase64String(hash);
    }
}
