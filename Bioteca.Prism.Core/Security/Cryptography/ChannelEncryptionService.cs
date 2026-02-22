using Bioteca.Prism.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Bioteca.Prism.Core.Security.Cryptography;

/// <summary>
/// Implementation of channel encryption using HKDF and AES-256-GCM
/// </summary>
public class ChannelEncryptionService : IChannelEncryptionService
{
    private readonly ILogger<ChannelEncryptionService> _logger;
    private const int NonceSize = 12; // 96 bits for AES-GCM
    private const int TagSize = 16; // 128 bits for AES-GCM

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
    };

    public ChannelEncryptionService(ILogger<ChannelEncryptionService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public byte[] DeriveKey(byte[] sharedSecret, byte[] salt, byte[] info, int keyLength = 32)
    {
        try
        {
            // 🔍 DEBUG: Log all derivation inputs
            _logger.LogInformation("=== HKDF Key Derivation Inputs ===");
            _logger.LogInformation("Shared Secret (Base64): {SharedSecret}", Convert.ToBase64String(sharedSecret));
            _logger.LogInformation("Shared Secret (Length): {Length} bytes", sharedSecret.Length);
            _logger.LogInformation("Salt (Base64): {Salt}", Convert.ToBase64String(salt));
            _logger.LogInformation("Salt (Length): {Length} bytes", salt.Length);
            _logger.LogInformation("Info Context (Base64): {Info}", Convert.ToBase64String(info));
            _logger.LogInformation("Info Context (String): {InfoString}", Encoding.UTF8.GetString(info));
            _logger.LogInformation("Info Context (Length): {Length} bytes", info.Length);

            using var hkdf = new HKDF(HashAlgorithmName.SHA256, sharedSecret, salt, info);
            var derivedKey = new byte[keyLength];
            hkdf.DeriveKey(derivedKey);

            // 🔍 DEBUG: Log derived key
            _logger.LogInformation("Derived Key (Base64): {DerivedKey}", Convert.ToBase64String(derivedKey));
            _logger.LogInformation("Derived Key (Length): {Length} bytes", derivedKey.Length);
            _logger.LogInformation("===================================");

            _logger.LogDebug("Derived {KeyLength}-byte key using HKDF-SHA256", keyLength);

            return derivedKey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to derive key using HKDF");
            throw;
        }
    }

    /// <inheritdoc/>
    public EncryptedData Encrypt(byte[] plaintext, byte[] key, byte[]? associatedData = null)
    {
        try
        {
            var nonce = new byte[NonceSize];
            RandomNumberGenerator.Fill(nonce);

            var ciphertext = new byte[plaintext.Length];
            var tag = new byte[TagSize];

            using var aesGcm = new AesGcm(key, TagSize);
            aesGcm.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);

            _logger.LogDebug("Encrypted {PlaintextLength} bytes with AES-256-GCM", plaintext.Length);

            return new EncryptedData
            {
                Nonce = nonce,
                Ciphertext = ciphertext,
                Tag = tag
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt data");
            throw;
        }
    }

    /// <inheritdoc/>
    public byte[] Decrypt(EncryptedData encryptedData, byte[] key, byte[]? associatedData = null)
    {
        try
        {
            var plaintext = new byte[encryptedData.Ciphertext.Length];

            using var aesGcm = new AesGcm(key, TagSize);
            aesGcm.Decrypt(encryptedData.Nonce, encryptedData.Ciphertext, encryptedData.Tag, plaintext, associatedData);

            _logger.LogDebug("Decrypted {CiphertextLength} bytes with AES-256-GCM", encryptedData.Ciphertext.Length);

            return plaintext;
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Failed to decrypt data - authentication failed");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt data");
            throw;
        }
    }

    /// <inheritdoc/>
    public string GenerateNonce(int length = 16)
    {
        var nonceBytes = new byte[length];
        RandomNumberGenerator.Fill(nonceBytes);
        var nonce = Convert.ToBase64String(nonceBytes);

        _logger.LogDebug("Generated {Length}-byte nonce", length);

        return nonce;
    }

    /// <inheritdoc/>
    public EncryptedPayload EncryptPayload(object payload, byte[] symmetricKey)
    {
        try
        {
            // 1. Serialize payload to JSON with relaxed escaping
            var jsonPayload = JsonSerializer.Serialize(payload, JsonOptions);
            var plaintextBytes = Encoding.UTF8.GetBytes(jsonPayload);

            _logger.LogDebug("Serialized payload to JSON ({Length} bytes)", plaintextBytes.Length);

            // 2. Encrypt with AES-256-GCM
            var encryptedData = Encrypt(plaintextBytes, symmetricKey);

            // 3. Return base64-encoded payload
            return new EncryptedPayload
            {
                EncryptedData = Convert.ToBase64String(encryptedData.Ciphertext),
                Iv = Convert.ToBase64String(encryptedData.Nonce),
                AuthTag = Convert.ToBase64String(encryptedData.Tag)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt payload");
            throw;
        }
    }

    /// <inheritdoc/>
    public T DecryptPayload<T>(EncryptedPayload encryptedPayload, byte[] symmetricKey)
    {
        try
        {
            // 1. Decode base64
            var ciphertext = Convert.FromBase64String(encryptedPayload.EncryptedData);
            var nonce = Convert.FromBase64String(encryptedPayload.Iv);
            var tag = Convert.FromBase64String(encryptedPayload.AuthTag);

            _logger.LogDebug("Decoded encrypted payload from base64");

            // 2. Decrypt with AES-256-GCM
            var encryptedData = new EncryptedData
            {
                Ciphertext = ciphertext,
                Nonce = nonce,
                Tag = tag
            };

            var plaintextBytes = Decrypt(encryptedData, symmetricKey);

            // 3. Deserialize JSON with relaxed options
            var jsonPayload = Encoding.UTF8.GetString(plaintextBytes);
            var result = JsonSerializer.Deserialize<T>(jsonPayload, JsonOptions);

            if (result == null)
            {
                throw new InvalidOperationException("Deserialization returned null");
            }

            _logger.LogDebug("Successfully decrypted and deserialized payload to {Type}", typeof(T).Name);

            return result;
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Failed to decrypt payload - authentication failed or wrong key");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt payload");
            throw;
        }
    }
}

/// <summary>
/// HKDF implementation wrapper for key derivation
/// </summary>
internal class HKDF : IDisposable
{
    private readonly HashAlgorithmName _hashAlgorithm;
    private readonly byte[] _prk;
    private readonly byte[] _info;
    private bool _disposed;

    public HKDF(HashAlgorithmName hashAlgorithm, byte[] inputKeyMaterial, byte[]? salt, byte[]? info)
    {
        _hashAlgorithm = hashAlgorithm;
        _info = info ?? Array.Empty<byte>();
        _prk = Extract(inputKeyMaterial, salt);
    }

    public void DeriveKey(Span<byte> output)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(HKDF));

        // HKDF-Expand
        using var hmac = IncrementalHash.CreateHMAC(_hashAlgorithm, _prk);
        var hashLength = _hashAlgorithm.Name switch
        {
            "SHA256" => 32,
            "SHA384" => 48,
            "SHA512" => 64,
            _ => throw new NotSupportedException($"Hash algorithm {_hashAlgorithm.Name} not supported")
        };

        var iterations = (output.Length + hashLength - 1) / hashLength;
        var t = Array.Empty<byte>();

        for (byte i = 1; i <= iterations; i++)
        {
            hmac.AppendData(t);
            hmac.AppendData(_info);  // RFC 5869 compliance
            hmac.AppendData(new[] { i });

            t = hmac.GetHashAndReset();
            var copyLength = Math.Min(hashLength, output.Length - (i - 1) * hashLength);
            t.AsSpan(0, copyLength).CopyTo(output.Slice((i - 1) * hashLength));
        }
    }

    private byte[] Extract(byte[] inputKeyMaterial, byte[]? salt)
    {
        // HKDF-Extract
        salt ??= new byte[_hashAlgorithm.Name switch
        {
            "SHA256" => 32,
            "SHA384" => 48,
            "SHA512" => 64,
            _ => 32
        }];

        using var hmac = IncrementalHash.CreateHMAC(_hashAlgorithm, salt);
        hmac.AppendData(inputKeyMaterial);
        return hmac.GetHashAndReset();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Array.Clear(_prk, 0, _prk.Length);
            Array.Clear(_info, 0, _info.Length);
            _disposed = true;
        }
    }
}
