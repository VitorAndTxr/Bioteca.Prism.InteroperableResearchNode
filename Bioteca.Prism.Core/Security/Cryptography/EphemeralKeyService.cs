using Bioteca.Prism.Core.Security.Cryptography.Interfaces;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace Bioteca.Prism.Core.Security.Cryptography;

/// <summary>
/// Implementation of ephemeral key management for ECDH
/// </summary>
public partial class EphemeralKeyService : IEphemeralKeyService
{
    private readonly ILogger<EphemeralKeyService> _logger;

    public EphemeralKeyService(ILogger<EphemeralKeyService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public ECDiffieHellman GenerateEphemeralKeyPair(string curve = "P384")
    {
        try
        {
            var ecdhCurve = GetECCurve(curve);
            var ecdh = ECDiffieHellman.Create(ecdhCurve);

            _logger.LogDebug("Generated ephemeral ECDH key pair with curve {Curve}", curve);

            return ecdh;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate ephemeral key pair with curve {Curve}", curve);
            throw;
        }
    }

    /// <inheritdoc/>
    public string ExportPublicKey(ECDiffieHellman ecdh)
    {
        try
        {
            var publicKey = ecdh.ExportSubjectPublicKeyInfo();
            var base64Key = Convert.ToBase64String(publicKey);

            _logger.LogDebug("Exported public key ({Length} bytes)", publicKey.Length);

            return base64Key;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export public key");
            throw;
        }
    }

    /// <inheritdoc/>
    public ECDiffieHellman ImportPublicKey(string base64PublicKey, string curve = "P384")
    {
        try
        {
            var publicKeyBytes = Convert.FromBase64String(base64PublicKey);
            var ecdh = ECDiffieHellman.Create();
            ecdh.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);

            _logger.LogDebug("Imported public key ({Length} bytes) with curve {Curve}", publicKeyBytes.Length, curve);

            return ecdh;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import public key");
            throw;
        }
    }

    /// <inheritdoc/>
    public byte[] DeriveSharedSecret(ECDiffieHellman localEcdh, ECDiffieHellman remoteEcdh)
    {
        try
        {
            var remotePublicKey = remoteEcdh.PublicKey;
            var sharedSecret = localEcdh.DeriveKeyMaterial(remotePublicKey);

            _logger.LogDebug("Derived shared secret ({Length} bytes)", sharedSecret.Length);

            return sharedSecret;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to derive shared secret");
            throw;
        }
    }

    /// <inheritdoc/>
    public bool ValidatePublicKey(string publicKeyBase64, string curve = "P384")
    {
        try
        {
            var publicKeyBytes = Convert.FromBase64String(publicKeyBase64);

            // Try to import the key - if it's invalid, this will throw
            using var ecdh = ECDiffieHellman.Create();
            ecdh.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);

            // Verify the key is on the expected curve
            var parameters = ecdh.ExportParameters(false);
            var expectedCurve = GetECCurve(curve);

            if (!CurvesMatch(parameters.Curve, expectedCurve))
            {
                _logger.LogWarning("Public key curve mismatch. Expected {Expected}, got different curve", curve);
                return false;
            }

            _logger.LogDebug("Public key validated successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Public key validation failed");
            return false;
        }
    }

    private ECCurve GetECCurve(string curveName)
    {
        return curveName.ToUpperInvariant() switch
        {
            "P256" => ECCurve.NamedCurves.nistP256,
            "P384" => ECCurve.NamedCurves.nistP384,
            "P521" => ECCurve.NamedCurves.nistP521,
            _ => throw new ArgumentException($"Unsupported curve: {curveName}", nameof(curveName))
        };
    }

    private bool CurvesMatch(ECCurve curve1, ECCurve curve2)
    {
        // Compare curve OIDs if available
        return curve1.Oid?.Value == curve2.Oid?.Value;
    }

}
