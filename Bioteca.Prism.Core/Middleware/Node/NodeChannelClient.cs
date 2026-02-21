using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net.Http.Json;
using Bioteca.Prism.Domain.Requests.Node;
using Bioteca.Prism.Domain.Errors.Node;
using Bioteca.Prism.Domain.Responses.Node;
using Bioteca.Prism.Core.Middleware.Channel;
using System.Net.Http;
using Bioteca.Prism.Core.Interfaces;

namespace Bioteca.Prism.Core.Middleware.Node;

/// <summary>
/// Client implementation for initiating channel handshake with remote nodes
/// </summary>
public class NodeChannelClient : INodeChannelClient
{
    private readonly ILogger<NodeChannelClient> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IEphemeralKeyService _ephemeralKeyService;
    private readonly IChannelEncryptionService _encryptionService;
    private readonly IConfiguration _configuration;

    private readonly IChannelStore _channelStore;

    public NodeChannelClient(
        ILogger<NodeChannelClient> logger,
        IHttpClientFactory httpClientFactory,
        IEphemeralKeyService ephemeralKeyService,
        IChannelEncryptionService encryptionService,
        IConfiguration configuration,
        IChannelStore channelStore)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _ephemeralKeyService = ephemeralKeyService;
        _encryptionService = encryptionService;
        _configuration = configuration;
        _channelStore = channelStore;
    }

    /// <inheritdoc/>
    public async Task<ChannelEstablishmentResult> OpenChannelAsync(string remoteNodeUrl)
    {
        try
        {
            _logger.LogInformation("Initiating channel handshake with {RemoteNodeUrl}", remoteNodeUrl);

            // Get configuration
            var keyExchangeAlgorithm = _configuration["ChannelSecurity:KeyExchangeAlgorithm"] ?? "ECDH-P384";
            var supportedCiphers = _configuration.GetSection("ChannelSecurity:SupportedCiphers").Get<List<string>>()
                ?? new List<string> { "AES-256-GCM", "ChaCha20-Poly1305" };

            // Extract curve from algorithm
            var curve = ExtractCurveFromAlgorithm(keyExchangeAlgorithm);

            // Generate ephemeral key pair
            var clientEcdh = _ephemeralKeyService.GenerateEphemeralKeyPair(curve);
            var clientPublicKey = _ephemeralKeyService.ExportPublicKey(clientEcdh);

            // Generate nonce
            var clientNonce = _encryptionService.GenerateNonce();

            // Create request
            var request = new ChannelOpenRequest
            {
                ProtocolVersion = "1.0",
                EphemeralPublicKey = clientPublicKey,
                KeyExchangeAlgorithm = keyExchangeAlgorithm,
                SupportedCiphers = supportedCiphers,
                Timestamp = DateTime.UtcNow,
                Nonce = clientNonce
            };

            // Send request to remote node
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.PostAsJsonAsync($"{remoteNodeUrl}/api/channel/open", request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadFromJsonAsync<Error>();
                _logger.LogWarning("Channel handshake failed: {ErrorCode} - {ErrorMessage}",
                    error?.ErrorDetail?.Code, error?.ErrorDetail?.Message);

                return new ChannelEstablishmentResult
                {
                    Success = false,
                    Error = error
                };
            }

            // Parse response
            var channelReady = await response.Content.ReadFromJsonAsync<ChannelReadyResponse>();
            if (channelReady == null)
            {
                return new ChannelEstablishmentResult
                {
                    Success = false,
                    Error = CreateError("ERR_CHANNEL_FAILED", "Invalid response from server")
                };
            }

            // Get channel ID from header
            var channelId = response.Headers.GetValues("X-Channel-Id").FirstOrDefault();
            if (string.IsNullOrEmpty(channelId))
            {
                return new ChannelEstablishmentResult
                {
                    Success = false,
                    Error = CreateError("ERR_CHANNEL_FAILED", "Server did not provide channel ID")
                };
            }

            // Validate server's ephemeral public key
            if (!_ephemeralKeyService.ValidatePublicKey(channelReady.EphemeralPublicKey, curve))
            {
                return new ChannelEstablishmentResult
                {
                    Success = false,
                    Error = CreateError("ERR_INVALID_EPHEMERAL_KEY", "Server's ephemeral public key is invalid")
                };
            }

            // Import server's public key
            var serverEcdh = _ephemeralKeyService.ImportPublicKey(channelReady.EphemeralPublicKey, curve);

            // Derive shared secret
            var sharedSecret = _ephemeralKeyService.DeriveSharedSecret(clientEcdh, serverEcdh);

            // Derive symmetric key using HKDF (same process as server)
            var salt = CombineNonces(clientNonce, channelReady.Nonce);
            var info = System.Text.Encoding.UTF8.GetBytes("IRN-Channel-v1.0");
            var symmetricKey = _encryptionService.DeriveKey(sharedSecret, salt, info);

            // Clean up ECDH objects
            clientEcdh.Dispose();
            serverEcdh.Dispose();
            Array.Clear(sharedSecret, 0, sharedSecret.Length);

            // Store channel context
            var channelContext = new ChannelContext
            {
                ChannelId = channelId,
                SymmetricKey = symmetricKey,
                SelectedCipher = channelReady.SelectedCipher,
                RemoteNodeUrl = remoteNodeUrl,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30),
                ClientNonce = clientNonce,
                ServerNonce = channelReady.Nonce,
                Role = "client"
            };

            await _channelStore.AddChannelAsync(channelId, channelContext);

            _logger.LogInformation("Channel {ChannelId} established successfully with {RemoteNodeUrl}",
                channelId, remoteNodeUrl);

            return new ChannelEstablishmentResult
            {
                Success = true,
                ChannelId = channelId,
                SymmetricKey = symmetricKey,
                SelectedCipher = channelReady.SelectedCipher,
                RemoteNodeUrl = remoteNodeUrl,
                ClientNonce = clientNonce,
                ServerNonce = channelReady.Nonce
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during channel handshake with {RemoteNodeUrl}", remoteNodeUrl);
            return new ChannelEstablishmentResult
            {
                Success = false,
                Error = CreateError("ERR_TIMEOUT", $"Failed to connect to remote node: {ex.Message}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during channel handshake with {RemoteNodeUrl}", remoteNodeUrl);
            return new ChannelEstablishmentResult
            {
                Success = false,
                Error = CreateError("ERR_CHANNEL_FAILED", $"Channel establishment failed: {ex.Message}")
            };
        }
    }

    /// <inheritdoc/>
    public async Task CloseChannelAsync(string channelId)
    {
        await _channelStore.RemoveChannelAsync(channelId);
    }

    /// <inheritdoc/>
    public async Task<NodeStatusResponse> IdentifyNodeAsync(string channelId, NodeIdentifyRequest request)
    {
        var channelContext = await _channelStore.GetChannelAsync(channelId);
        if (channelContext == null)
        {
            throw new InvalidOperationException($"Channel {channelId} not found or expired");
        }

        // Encrypt payload
        var encryptedPayload = _encryptionService.EncryptPayload(request, channelContext.SymmetricKey);

        // Send request
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Add("X-Channel-Id", channelId);

        var response = await httpClient.PostAsJsonAsync($"{channelContext.RemoteNodeUrl}/api/node/identify", encryptedPayload);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<Error>();
            throw new Exception($"Identification failed: {error?.ErrorDetail?.Message}");
        }

        // Decrypt response
        var encryptedResponse = await response.Content.ReadFromJsonAsync<EncryptedPayload>();
        if (encryptedResponse == null)
        {
            throw new Exception("Failed to deserialize encrypted response");
        }

        return _encryptionService.DecryptPayload<NodeStatusResponse>(encryptedResponse, channelContext.SymmetricKey);
    }

    /// <inheritdoc/>
    public async Task<NodeRegistrationResponse> RegisterNodeAsync(string channelId, NodeRegistrationRequest request)
    {
        var channelContext = await _channelStore.GetChannelAsync(channelId);
        if (channelContext == null)
        {
            throw new InvalidOperationException($"Channel {channelId} not found or expired");
        }

        // Encrypt payload
        var encryptedPayload = _encryptionService.EncryptPayload(request, channelContext.SymmetricKey);

        // Send request
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Add("X-Channel-Id", channelId);

        var response = await httpClient.PostAsJsonAsync($"{channelContext.RemoteNodeUrl}/api/node/register", encryptedPayload);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<Error>();
            throw new Exception($"Registration failed: {error?.ErrorDetail?.Message}");
        }

        // Decrypt response
        var encryptedResponse = await response.Content.ReadFromJsonAsync<EncryptedPayload>();
        if (encryptedResponse == null)
        {
            throw new Exception("Failed to deserialize encrypted response");
        }

        return _encryptionService.DecryptPayload<NodeRegistrationResponse>(encryptedResponse, channelContext.SymmetricKey);
    }

    /// <inheritdoc/>
    public async Task<ChallengeResponse> RequestChallengeAsync(string channelId, string nodeId)
    {
        var channelContext = await _channelStore.GetChannelAsync(channelId);
        if (channelContext == null)
        {
            throw new InvalidOperationException($"Channel {channelId} not found or expired");
        }

        var request = new ChallengeRequest
        {
            ChannelId = channelId,
            NodeId = nodeId,
            Timestamp = DateTime.UtcNow
        };

        // Encrypt payload
        var encryptedPayload = _encryptionService.EncryptPayload(request, channelContext.SymmetricKey);

        // Send request
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Add("X-Channel-Id", channelId);

        var response = await httpClient.PostAsJsonAsync($"{channelContext.RemoteNodeUrl}/api/node/challenge", encryptedPayload);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<Error>();
            throw new Exception($"Challenge request failed: {error?.ErrorDetail?.Message}");
        }

        // Decrypt response
        var encryptedResponse = await response.Content.ReadFromJsonAsync<EncryptedPayload>();
        if (encryptedResponse == null)
        {
            throw new Exception("Failed to deserialize encrypted response");
        }

        return _encryptionService.DecryptPayload<ChallengeResponse>(encryptedResponse, channelContext.SymmetricKey);
    }

    /// <inheritdoc/>
    public async Task<AuthenticationResponse> AuthenticateAsync(string channelId, ChallengeResponseRequest request)
    {
        var channelContext = await _channelStore.GetChannelAsync(channelId);
        if (channelContext == null)
        {
            throw new InvalidOperationException($"Channel {channelId} not found or expired");
        }

        // Encrypt payload
        var encryptedPayload = _encryptionService.EncryptPayload(request, channelContext.SymmetricKey);

        // Send request
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Add("X-Channel-Id", channelId);

        var response = await httpClient.PostAsJsonAsync($"{channelContext.RemoteNodeUrl}/api/node/authenticate", encryptedPayload);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<Error>();
            throw new Exception($"Authentication failed: {error?.ErrorDetail?.Message}");
        }

        // Decrypt response
        var encryptedResponse = await response.Content.ReadFromJsonAsync<EncryptedPayload>();
        if (encryptedResponse == null)
        {
            throw new Exception("Failed to deserialize encrypted response");
        }

        return _encryptionService.DecryptPayload<AuthenticationResponse>(encryptedResponse, channelContext.SymmetricKey);
    }

    /// <inheritdoc/>
    public async Task<TResponse> InvokeAsync<TResponse>(
        string channelId,
        string sessionToken,
        HttpMethod method,
        string path,
        object? body = null)
    {
        var channelContext = await _channelStore.GetChannelAsync(channelId)
            ?? throw new InvalidOperationException($"Channel {channelId} not found or expired");

        var httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Add("X-Channel-Id", channelId);
        httpClient.DefaultRequestHeaders.Add("X-Session-Id", sessionToken);

        HttpRequestMessage request;
        if (body != null)
        {
            var encryptedPayload = _encryptionService.EncryptPayload(body, channelContext.SymmetricKey);
            request = new HttpRequestMessage(method, $"{channelContext.RemoteNodeUrl}{path}")
            {
                Content = JsonContent.Create(encryptedPayload)
            };
        }
        else
        {
            request = new HttpRequestMessage(method, $"{channelContext.RemoteNodeUrl}{path}");
        }

        var response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<Error>();
            throw new Exception($"Sync request failed [{response.StatusCode}]: {error?.ErrorDetail?.Message}");
        }

        var encryptedResponse = await response.Content.ReadFromJsonAsync<EncryptedPayload>();
        if (encryptedResponse == null)
        {
            throw new Exception("Failed to deserialize encrypted response");
        }

        return _encryptionService.DecryptPayload<TResponse>(encryptedResponse, channelContext.SymmetricKey);
    }

    /// <inheritdoc/>
    public async Task<Stream> InvokeStreamAsync(
        string channelId,
        string sessionToken,
        string path)
    {
        var channelContext = await _channelStore.GetChannelAsync(channelId)
            ?? throw new InvalidOperationException($"Channel {channelId} not found or expired");

        var httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Add("X-Channel-Id", channelId);
        httpClient.DefaultRequestHeaders.Add("X-Session-Id", sessionToken);

        var request = new HttpRequestMessage(HttpMethod.Get, $"{channelContext.RemoteNodeUrl}{path}");
        var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<Error>();
            throw new Exception($"Stream request failed [{response.StatusCode}]: {error?.ErrorDetail?.Message}");
        }

        // Return raw stream; caller is responsible for disposing
        return await response.Content.ReadAsStreamAsync();
    }

    private string ExtractCurveFromAlgorithm(string algorithm)
    {
        // Extract curve from "ECDH-P384" -> "P384"
        var parts = algorithm.Split('-');
        return parts.Length > 1 ? parts[1] : "P384";
    }

    private byte[] CombineNonces(string nonce1, string nonce2)
    {
        var bytes1 = Convert.FromBase64String(nonce1);
        var bytes2 = Convert.FromBase64String(nonce2);
        var combined = new byte[bytes1.Length + bytes2.Length];
        Buffer.BlockCopy(bytes1, 0, combined, 0, bytes1.Length);
        Buffer.BlockCopy(bytes2, 0, combined, bytes1.Length, bytes2.Length);
        return combined;
    }

    private Error CreateError(string code, string message)
    {
        return new Error
        {
            ErrorDetail = new ErrorDetails
            {
                Code = code,
                Message = message,
                Retryable = false
            }
        };
    }
}
