using Bioteca.Prism.Service.Interfaces.Node;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace Bioteca.Prism.Service.Services.Node;

/// <summary>
/// Implementation of channel encryption using HKDF and AES-256-GCM
/// </summary>
public class ChannelEncryptionService : IChannelEncryptionService
{
    private readonly ILogger<ChannelEncryptionService> _logger;
    private const int NonceSize = 12; // 96 bits for AES-GCM
    private const int TagSize = 16; // 128 bits for AES-GCM

    public ChannelEncryptionService(ILogger<ChannelEncryptionService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public byte[] DeriveKey(byte[] sharedSecret, byte[] salt, byte[] info, int keyLength = 32)
    {
        try
        {
            using var hkdf = new HKDF(HashAlgorithmName.SHA256, sharedSecret, salt, info);
            var derivedKey = new byte[keyLength];
            hkdf.DeriveKey(derivedKey);

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
}

/// <summary>
/// HKDF implementation wrapper for key derivation
/// </summary>
internal class HKDF : IDisposable
{
    private readonly HashAlgorithmName _hashAlgorithm;
    private readonly byte[] _prk;
    private bool _disposed;

    public HKDF(HashAlgorithmName hashAlgorithm, byte[] inputKeyMaterial, byte[]? salt, byte[]? info)
    {
        _hashAlgorithm = hashAlgorithm;
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
            _disposed = true;
        }
    }
}
