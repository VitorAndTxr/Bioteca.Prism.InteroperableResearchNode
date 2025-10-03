using System.Text.Json.Serialization;

namespace Bioteca.Prism.Core.Security.Cryptography.Interfaces;

/// <summary>
/// Service for channel encryption using HKDF key derivation and AES-GCM
/// </summary>
public interface IChannelEncryptionService
{
    /// <summary>
    /// Derive symmetric encryption keys from shared secret using HKDF
    /// </summary>
    /// <param name="sharedSecret">Shared secret from ECDH</param>
    /// <param name="salt">Salt for HKDF (nonces from both parties)</param>
    /// <param name="info">Context information for HKDF</param>
    /// <param name="keyLength">Desired key length in bytes (default: 32 for AES-256)</param>
    /// <returns>Derived symmetric key</returns>
    byte[] DeriveKey(byte[] sharedSecret, byte[] salt, byte[] info, int keyLength = 32);

    /// <summary>
    /// Encrypt data using AES-256-GCM
    /// </summary>
    /// <param name="plaintext">Data to encrypt</param>
    /// <param name="key">Symmetric encryption key</param>
    /// <param name="associatedData">Additional authenticated data (AAD)</param>
    /// <returns>Encrypted data with nonce and tag</returns>
    EncryptedData Encrypt(byte[] plaintext, byte[] key, byte[]? associatedData = null);

    /// <summary>
    /// Decrypt data using AES-256-GCM
    /// </summary>
    /// <param name="encryptedData">Encrypted data with nonce and tag</param>
    /// <param name="key">Symmetric encryption key</param>
    /// <param name="associatedData">Additional authenticated data (AAD)</param>
    /// <returns>Decrypted plaintext</returns>
    byte[] Decrypt(EncryptedData encryptedData, byte[] key, byte[]? associatedData = null);

    /// <summary>
    /// Generate a cryptographically secure random nonce
    /// </summary>
    /// <param name="length">Nonce length in bytes (default: 16)</param>
    /// <returns>Random nonce</returns>
    string GenerateNonce(int length = 16);

    /// <summary>
    /// Encrypt a payload object to JSON and encrypt it
    /// </summary>
    /// <param name="payload">Object to encrypt</param>
    /// <param name="symmetricKey">Symmetric key from channel</param>
    /// <returns>Encrypted payload with base64-encoded data</returns>
    EncryptedPayload EncryptPayload(object payload, byte[] symmetricKey);

    /// <summary>
    /// Decrypt an encrypted payload and deserialize to object
    /// </summary>
    /// <typeparam name="T">Type to deserialize to</typeparam>
    /// <param name="encryptedPayload">Encrypted payload</param>
    /// <param name="symmetricKey">Symmetric key from channel</param>
    /// <returns>Decrypted and deserialized object</returns>
    T DecryptPayload<T>(EncryptedPayload encryptedPayload, byte[] symmetricKey);
}

/// <summary>
/// Encrypted data container with nonce and authentication tag
/// </summary>
public class EncryptedData
{
    /// <summary>
    /// Initialization vector (nonce) used for encryption
    /// </summary>
    public byte[] Nonce { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Encrypted ciphertext
    /// </summary>
    public byte[] Ciphertext { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Authentication tag for GCM mode
    /// </summary>
    public byte[] Tag { get; set; } = Array.Empty<byte>();
}

/// <summary>
/// Encrypted payload with base64-encoded data for JSON serialization
/// </summary>
public class EncryptedPayload
{
    /// <summary>
    /// Base64-encoded encrypted data
    /// </summary>
    [JsonPropertyName("encryptedData")]
    public string EncryptedData { get; set; } = string.Empty;

    /// <summary>
    /// Base64-encoded initialization vector
    /// </summary>
    [JsonPropertyName("iv")]
    public string Iv { get; set; } = string.Empty;

    /// <summary>
    /// Base64-encoded authentication tag
    /// </summary>
    [JsonPropertyName("authTag")]
    public string AuthTag { get; set; } = string.Empty;
}
