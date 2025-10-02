using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Bioteca.Prism.Service.Services.Node;

/// <summary>
/// Helper class for certificate generation and signing (for testing purposes)
/// </summary>
public static class CertificateHelper
{
    /// <summary>
    /// Generate a self-signed certificate for testing
    /// </summary>
    /// <param name="subjectName">Subject name (CN)</param>
    /// <param name="validityYears">Validity period in years</param>
    /// <returns>X509Certificate2 with private key</returns>
    public static X509Certificate2 GenerateSelfSignedCertificate(string subjectName, int validityYears = 2)
    {
        using var rsa = RSA.Create(2048);

        var request = new CertificateRequest(
            $"CN={subjectName}",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        // Add basic constraints
        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(false, false, 0, false));

        // Add key usage
        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                false));

        // Add enhanced key usage
        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(
                new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, // Server Authentication
                false));

        var notBefore = DateTimeOffset.UtcNow;
        var notAfter = notBefore.AddYears(validityYears);

        var certificate = request.CreateSelfSigned(notBefore, notAfter);

        return certificate;
    }

    /// <summary>
    /// Export certificate to Base64 string
    /// </summary>
    public static string ExportCertificateToBase64(X509Certificate2 certificate)
    {
        return Convert.ToBase64String(certificate.Export(X509ContentType.Cert));
    }

    /// <summary>
    /// Export certificate with private key to Base64 string (PFX format)
    /// </summary>
    public static string ExportCertificateWithPrivateKeyToBase64(X509Certificate2 certificate, string password)
    {
        return Convert.ToBase64String(certificate.Export(X509ContentType.Pfx, password));
    }

    /// <summary>
    /// Sign data with certificate's private key
    /// </summary>
    public static string SignData(string data, X509Certificate2 certificate)
    {
        using var rsa = certificate.GetRSAPrivateKey();
        if (rsa == null)
        {
            throw new InvalidOperationException("Certificate does not contain RSA private key");
        }

        var dataBytes = System.Text.Encoding.UTF8.GetBytes(data);
        var signature = rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        return Convert.ToBase64String(signature);
    }

    /// <summary>
    /// Verify signature with certificate's public key
    /// </summary>
    public static bool VerifySignature(string data, string signatureBase64, X509Certificate2 certificate)
    {
        using var rsa = certificate.GetRSAPublicKey();
        if (rsa == null)
        {
            return false;
        }

        var dataBytes = System.Text.Encoding.UTF8.GetBytes(data);
        var signature = Convert.FromBase64String(signatureBase64);

        return rsa.VerifyData(dataBytes, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }

    /// <summary>
    /// Load certificate from Base64 string
    /// </summary>
    public static X509Certificate2 LoadCertificateFromBase64(string base64Certificate)
    {
        var certBytes = Convert.FromBase64String(base64Certificate);
        return new X509Certificate2(certBytes);
    }

    /// <summary>
    /// Load certificate with private key from Base64 PFX
    /// </summary>
    public static X509Certificate2 LoadCertificateWithPrivateKeyFromBase64(string base64Pfx, string password)
    {
        var pfxBytes = Convert.FromBase64String(base64Pfx);
        return new X509Certificate2(pfxBytes, password, X509KeyStorageFlags.Exportable);
    }
}
