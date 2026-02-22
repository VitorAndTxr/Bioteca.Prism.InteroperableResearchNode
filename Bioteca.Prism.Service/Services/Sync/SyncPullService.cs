using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Bioteca.Prism.Core.Middleware.Node;
using Bioteca.Prism.Data.Interfaces.Node;
using Bioteca.Prism.Data.Interfaces.Sync;
using Bioteca.Prism.Domain.DTOs.Sync;
using Bioteca.Prism.Domain.Requests.Node;
using Bioteca.Prism.Domain.Responses.Node;
using Bioteca.Prism.Service.Interfaces.Sync;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Bioteca.Prism.Service.Services.Sync;

/// <summary>
/// Orchestrates the pull-based node-to-node data synchronization flow from the backend.
/// Resolves the remote node URL, performs the 4-phase handshake via INodeChannelClient,
/// fetches all entity pages in dependency order, and calls SyncImportService for
/// transactional upsert. Channel is always closed in a finally block.
/// </summary>
public class SyncPullService : ISyncPullService
{
    private const int PageSize = 100;

    private static readonly string[] SnomedEntityTypes =
    [
        "body-regions",
        "body-structures",
        "topographical-modifiers",
        "lateralities",
        "clinical-conditions",
        "clinical-events",
        "medications",
        "allergy-intolerances",
        "severity-codes"
    ];

    private readonly INodeChannelClient _nodeChannelClient;
    private readonly INodeRepository _nodeRepository;
    private readonly ISyncLogRepository _syncLogRepository;
    private readonly ISyncImportService _syncImportService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SyncPullService> _logger;

    public SyncPullService(
        INodeChannelClient nodeChannelClient,
        INodeRepository nodeRepository,
        ISyncLogRepository syncLogRepository,
        ISyncImportService syncImportService,
        IConfiguration configuration,
        ILogger<SyncPullService> logger)
    {
        _nodeChannelClient = nodeChannelClient;
        _nodeRepository = nodeRepository;
        _syncLogRepository = syncLogRepository;
        _syncImportService = syncImportService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<SyncPreviewResponse> PreviewAsync(
        Guid remoteNodeId, DateTime? since,
        CancellationToken cancellationToken = default)
    {
        var (remoteNode, resolvedSince) = await ResolveNodeAndSinceAsync(remoteNodeId, since, cancellationToken);

        string? channelId = null;
        try
        {
            var (channel, sessionToken) = await PerformHandshakeAsync(remoteNode.NodeUrl);
            channelId = channel;

            var manifestRequest = new { since = resolvedSince?.ToString("O") };
            var manifest = await _nodeChannelClient.InvokeAsync<SyncManifestResponse>(
                channelId, sessionToken, HttpMethod.Post, "/api/sync/manifest", manifestRequest);

            return new SyncPreviewResponse
            {
                Manifest = manifest,
                AutoResolvedSince = resolvedSince,
                RemoteNodeId = remoteNodeId
            };
        }
        finally
        {
            if (channelId != null)
                await CloseChannelSafeAsync(channelId);
        }
    }

    /// <inheritdoc/>
    public async Task<SyncResultDTO> PullAsync(
        Guid remoteNodeId, DateTime? since,
        CancellationToken cancellationToken = default)
    {
        var (remoteNode, resolvedSince) = await ResolveNodeAndSinceAsync(remoteNodeId, since, cancellationToken);

        string? channelId = null;
        try
        {
            var (channel, sessionToken) = await PerformHandshakeAsync(remoteNode.NodeUrl);
            channelId = channel;

            _logger.LogInformation(
                "Starting sync pull from remote node {RemoteNodeId} ({NodeUrl}) since {Since}",
                remoteNodeId, remoteNode.NodeUrl, resolvedSince?.ToString("O") ?? "beginning");

            var manifestRequest = new { since = resolvedSince?.ToString("O") };
            var manifest = await _nodeChannelClient.InvokeAsync<SyncManifestResponse>(
                channelId, sessionToken, HttpMethod.Post, "/api/sync/manifest", manifestRequest);

            var payload = await FetchAllEntitiesAsync(channelId, sessionToken, resolvedSince, manifest.GeneratedAt, remoteNodeId);

            return await _syncImportService.ImportAsync(payload, remoteNodeId);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Sync pull failed for remote node {RemoteNodeId}", remoteNodeId);
            throw;
        }
        finally
        {
            if (channelId != null)
                await CloseChannelSafeAsync(channelId);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Handshake
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<(string ChannelId, string SessionToken)> PerformHandshakeAsync(string remoteNodeUrl)
    {
        // Phase 1 — ECDH key exchange
        var channelResult = await _nodeChannelClient.OpenChannelAsync(remoteNodeUrl);
        if (!channelResult.Success || string.IsNullOrEmpty(channelResult.ChannelId))
        {
            throw new InvalidOperationException(
                $"ERR_HANDSHAKE_FAILED: Failed to open channel with {remoteNodeUrl}. " +
                $"{channelResult.Error?.ErrorDetail?.Message}");
        }

        var channelId = channelResult.ChannelId;
        var localNodeId = _configuration["NodeSecurity:NodeId"]
            ?? throw new InvalidOperationException("NodeSecurity:NodeId is not configured");
        var certBase64 = _configuration["NodeSecurity:Certificate"]
            ?? throw new InvalidOperationException("NodeSecurity:Certificate is not configured");

        // Phase 2 — Node identification
        var timestamp = DateTime.UtcNow;
        var identifySignature = SignIdentifyData(channelId, localNodeId, timestamp);

        var identifyRequest = new NodeIdentifyRequest
        {
            ChannelId = channelId,
            NodeId = localNodeId,
            NodeName = _configuration["NodeSecurity:NodeName"] ?? localNodeId,
            Certificate = certBase64,
            Timestamp = timestamp,
            Signature = identifySignature
        };

        var nodeStatus = await _nodeChannelClient.IdentifyNodeAsync(channelId, identifyRequest);

        if (nodeStatus.Status != AuthorizationStatus.Authorized)
        {
            throw new InvalidOperationException(
                $"ERR_AUTHENTICATION_FAILED: Remote node rejected identification. " +
                $"Status: {nodeStatus.Status}. {nodeStatus.Message}");
        }

        // Phase 3a — Request challenge
        var challengeResponse = await _nodeChannelClient.RequestChallengeAsync(channelId, localNodeId);

        // Phase 3b — Sign challenge and authenticate
        var challengeTimestamp = DateTime.UtcNow;
        var challengeSignature = SignChallengeData(
            challengeResponse.ChallengeData, channelId, localNodeId, challengeTimestamp);

        var authRequest = new ChallengeResponseRequest
        {
            ChannelId = channelId,
            NodeId = localNodeId,
            ChallengeData = challengeResponse.ChallengeData,
            Signature = challengeSignature,
            Timestamp = challengeTimestamp
        };

        var authResult = await _nodeChannelClient.AuthenticateAsync(channelId, authRequest);

        if (!authResult.Authenticated || string.IsNullOrEmpty(authResult.SessionToken))
        {
            throw new InvalidOperationException(
                $"ERR_AUTHENTICATION_FAILED: Authentication rejected by remote node. {authResult.Message}");
        }

        _logger.LogInformation("Handshake completed with channel {ChannelId}", channelId);
        return (channelId, authResult.SessionToken);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Entity fetch
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<SyncImportPayload> FetchAllEntitiesAsync(
        string channelId, string sessionToken, DateTime? since, DateTime manifestGeneratedAt, Guid remoteNodeId)
    {
        var sinceParam = since?.ToString("O");

        var payload = new SyncImportPayload
        {
            ManifestGeneratedAt = manifestGeneratedAt,
            RemoteNodeId = remoteNodeId,
            Snomed = new SyncSnomedPayload()
        };

        // SNOMED catalog (9 sub-types)
        foreach (var entityType in SnomedEntityTypes)
        {
            var entities = await FetchAllPagesAsync(channelId, sessionToken, $"/api/sync/snomed/{entityType}", sinceParam);
            AssignSnomedEntities(payload.Snomed, entityType, entities);
        }

        // Volunteers → Research → Sessions (dependency order)
        payload.Volunteers = await FetchAllPagesAsync(channelId, sessionToken, "/api/sync/volunteers", sinceParam);

        // Researchers and Devices (FK dependencies for Research join tables)
        payload.Researchers = await FetchAllPagesAsync(channelId, sessionToken, "/api/sync/researchers", sinceParam);
        payload.Devices = await FetchAllPagesAsync(channelId, sessionToken, "/api/sync/devices", sinceParam);

        payload.Research = await FetchAllPagesAsync(channelId, sessionToken, "/api/sync/research", sinceParam);
        payload.Sessions = await FetchAllPagesAsync(channelId, sessionToken, "/api/sync/sessions", sinceParam);

        // Recording files for channels that have a file reference (non-fatal: skip files that fail)
        var recordingIds = ExtractRecordingChannelIds(payload.Sessions);
        foreach (var id in recordingIds)
        {
            try
            {
                var fileEntry = await _nodeChannelClient.InvokeAsync<RecordingFileResponseDto>(
                    channelId, sessionToken, HttpMethod.Get, $"/api/sync/recordings/{id}/file");

                payload.Recordings.Add(new RecordingFileEntry
                {
                    Id = Guid.Parse(id),
                    ContentBase64 = fileEntry.ContentBase64,
                    ContentType = fileEntry.ContentType,
                    FileName = fileEntry.FileName
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Skipping recording file {RecordChannelId} — fetch failed", id);
            }
        }

        return payload;
    }

    private async Task<List<JsonElement>> FetchAllPagesAsync(
        string channelId, string sessionToken, string basePath, string? since)
    {
        var all = new List<JsonElement>();
        var page = 1;
        var totalPages = 1;

        do
        {
            var queryString = $"?page={page}&pageSize={PageSize}";
            if (!string.IsNullOrEmpty(since))
                queryString += $"&since={Uri.EscapeDataString(since)}";

            var response = await _nodeChannelClient.InvokeAsync<PagedSyncResult<JsonElement>>(
                channelId, sessionToken, HttpMethod.Get, $"{basePath}{queryString}");

            all.AddRange(response.Data);
            totalPages = response.TotalPages;
            page++;
        }
        while (page <= totalPages);

        return all;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Signing helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Signs the identification payload: {channelId}{nodeId}{timestamp:O}
    /// Mirrors the signature format used in NodeChannelClientTests.
    /// </summary>
    private string SignIdentifyData(string channelId, string nodeId, DateTime timestamp)
    {
        var data = $"{channelId}{nodeId}{timestamp:O}";
        return SignWithPrivateKey(data);
    }

    /// <summary>
    /// Signs the challenge response payload: {challengeData}{channelId}{nodeId}{timestamp:O}
    /// </summary>
    private string SignChallengeData(string challengeData, string channelId, string nodeId, DateTime timestamp)
    {
        var data = $"{challengeData}{channelId}{nodeId}{timestamp:O}";
        return SignWithPrivateKey(data);
    }

    private string SignWithPrivateKey(string data)
    {
        var pfxBase64 = _configuration["NodeSecurity:PrivateKeyPfx"]
            ?? throw new InvalidOperationException("NodeSecurity:PrivateKeyPfx is not configured");

        var pfxBytes = Convert.FromBase64String(pfxBase64);
        var pfxPassword = _configuration["NodeSecurity:PrivateKeyPfxPassword"] ?? string.Empty;

        using var certificate = new X509Certificate2(pfxBytes, pfxPassword, X509KeyStorageFlags.Exportable);
        using var rsa = certificate.GetRSAPrivateKey()
            ?? throw new InvalidOperationException("Certificate does not contain an RSA private key");

        var dataBytes = System.Text.Encoding.UTF8.GetBytes(data);
        var signature = rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return Convert.ToBase64String(signature);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<(Domain.Entities.Node.ResearchNode node, DateTime? since)> ResolveNodeAndSinceAsync(
        Guid remoteNodeId, DateTime? since, CancellationToken cancellationToken)
    {
        var node = await _nodeRepository.GetByIdAsync(remoteNodeId, cancellationToken);
        if (node == null)
        {
            throw new InvalidOperationException($"ERR_NODE_NOT_FOUND: Node {remoteNodeId} not found in registry.");
        }

        if (node.Status != AuthorizationStatus.Authorized)
        {
            throw new InvalidOperationException(
                $"ERR_NODE_NOT_FOUND: Node {remoteNodeId} is not authorized (Status: {node.Status}).");
        }

        if (string.IsNullOrEmpty(node.NodeUrl))
        {
            throw new InvalidOperationException($"ERR_NODE_NOT_FOUND: Node {remoteNodeId} has no URL configured.");
        }

        var resolvedSince = since;
        if (resolvedSince == null)
        {
            var latestLog = await _syncLogRepository.GetLatestCompletedAsync(remoteNodeId, cancellationToken);
            resolvedSince = latestLog?.LastSyncedAt;
        }

        return (node, resolvedSince);
    }

    private async Task CloseChannelSafeAsync(string channelId)
    {
        try
        {
            await _nodeChannelClient.CloseChannelAsync(channelId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to close channel {ChannelId} after sync", channelId);
        }
    }

    private static List<string> ExtractRecordingChannelIds(List<JsonElement> sessions)
    {
        var ids = new List<string>();

        foreach (var session in sessions)
        {
            if (!session.TryGetProperty("records", out var records)) continue;
            foreach (var record in records.EnumerateArray())
            {
                if (!record.TryGetProperty("recordChannels", out var channels)) continue;
                foreach (var channel in channels.EnumerateArray())
                {
                    if (channel.TryGetProperty("fileUrl", out var fileUrl)
                        && fileUrl.ValueKind == JsonValueKind.String
                        && !string.IsNullOrEmpty(fileUrl.GetString())
                        && channel.TryGetProperty("id", out var id))
                    {
                        ids.Add(id.GetString()!);
                    }
                }
            }
        }

        return ids;
    }

    private static void AssignSnomedEntities(SyncSnomedPayload payload, string entityType, List<JsonElement> entities)
    {
        switch (entityType)
        {
            case "body-regions": payload.BodyRegions.AddRange(entities); break;
            case "body-structures": payload.BodyStructures.AddRange(entities); break;
            case "topographical-modifiers": payload.TopographicalModifiers.AddRange(entities); break;
            case "lateralities": payload.Lateralities.AddRange(entities); break;
            case "clinical-conditions": payload.ClinicalConditions.AddRange(entities); break;
            case "clinical-events": payload.ClinicalEvents.AddRange(entities); break;
            case "medications": payload.Medications.AddRange(entities); break;
            case "allergy-intolerances": payload.AllergyIntolerances.AddRange(entities); break;
            case "severity-codes": payload.SeverityCodes.AddRange(entities); break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Recording file response DTO (matches remote endpoint response shape)
    // ─────────────────────────────────────────────────────────────────────────

    private sealed class RecordingFileResponseDto
    {
        public string ContentBase64 { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/octet-stream";
        public string FileName { get; set; } = string.Empty;
    }
}
