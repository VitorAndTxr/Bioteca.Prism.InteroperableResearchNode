using System.Security.Claims;

namespace Bioteca.Prism.Core.Interfaces;

/// <summary>
/// Interface para a classe de JWT handling
/// </summary>
public interface IJwtUtil
{
    /// <summary>
    /// Creates a JWT using the "system default" private key
    /// </summary>
    string CreateJwt(List<Claim> claims);

    /// <summary>
    /// Creates a JWT
    /// </summary>
    string CreateJwt(List<Claim> claims, string privateRsaKey);

    /// <summary>
    /// Decodes a JWT checking for the signature using the "system default" public key to validate the signer
    /// </summary>
    string DecodeJwt(string jwt);

    /// <summary>
    /// Decodes a JWT checking for the signature using the public key of the signer
    /// </summary>
    string DecodeJwt(string jwt, string publicRsaKey);
}
