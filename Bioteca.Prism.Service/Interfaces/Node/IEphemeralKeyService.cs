using System.Security.Cryptography;

namespace Bioteca.Prism.Service.Interfaces.Node;

/// <summary>
/// Service for generating and managing ephemeral ECDH keys
/// </summary>
public interface IEphemeralKeyService
{
    /// <summary>
    /// Generate a new ephemeral key pair for ECDH
    /// </summary>
    /// <param name="curve">EC curve name (e.g., "P384")</param>
    /// <returns>ECDiffieHellman instance with generated keys</returns>
    ECDiffieHellman GenerateEphemeralKeyPair(string curve = "P384");

    /// <summary>
    /// Export public key to base64 string
    /// </summary>
    /// <param name="ecdh">ECDH instance</param>
    /// <returns>Base64-encoded public key</returns>
    string ExportPublicKey(ECDiffieHellman ecdh);

    /// <summary>
    /// Import public key from base64 string
    /// </summary>
    /// <param name="base64PublicKey">Base64-encoded public key</param>
    /// <param name="curve">EC curve name</param>
    /// <returns>ECDiffieHellman instance with imported public key</returns>
    ECDiffieHellman ImportPublicKey(string base64PublicKey, string curve = "P384");

    /// <summary>
    /// Derive shared secret from local private key and remote public key
    /// </summary>
    /// <param name="localEcdh">Local ECDH instance with private key</param>
    /// <param name="remoteEcdh">Remote ECDH instance with public key</param>
    /// <returns>Shared secret bytes</returns>
    byte[] DeriveSharedSecret(ECDiffieHellman localEcdh, ECDiffieHellman remoteEcdh);

    /// <summary>
    /// Validate that a public key is on the curve and not the point at infinity
    /// </summary>
    /// <param name="publicKeyBase64">Base64-encoded public key</param>
    /// <param name="curve">EC curve name</param>
    /// <returns>True if valid, false otherwise</returns>
    bool ValidatePublicKey(string publicKeyBase64, string curve = "P384");
}
