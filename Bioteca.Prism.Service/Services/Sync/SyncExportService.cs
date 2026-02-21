using Azure.Storage.Blobs;
using Bioteca.Prism.Data.Interfaces.Node;
using Bioteca.Prism.Data.Interfaces.Sync;
using Bioteca.Prism.Data.Persistence.Contexts;
using Bioteca.Prism.Domain.DTOs.Sync;
using Bioteca.Prism.Service.Interfaces.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VolunteerEntity = Bioteca.Prism.Domain.Entities.Volunteer.Volunteer;

namespace Bioteca.Prism.Service.Services.Sync;

/// <summary>
/// Exports entities from this node for consumption by a requesting node (pull sync model).
/// Uses PrismDbContext directly for read-only queries with since-filtering and eager loading.
/// </summary>
public class SyncExportService : ISyncExportService
{
    private readonly PrismDbContext _context;
    private readonly INodeRepository _nodeRepository;
    private readonly ISyncLogRepository _syncLogRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SyncExportService> _logger;

    public SyncExportService(
        PrismDbContext context,
        INodeRepository nodeRepository,
        ISyncLogRepository syncLogRepository,
        IConfiguration configuration,
        ILogger<SyncExportService> logger)
    {
        _context = context;
        _nodeRepository = nodeRepository;
        _syncLogRepository = syncLogRepository;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<SyncManifestResponse> GetManifestAsync(DateTime? since, CancellationToken cancellationToken = default)
    {
        var nodes = await _nodeRepository.GetAllAsync(cancellationToken);
        var localNode = nodes.FirstOrDefault();

        // Count each SNOMED entity type separately with optional since-filter, then sum
        var snomedCount = 0;
        snomedCount += since.HasValue
            ? await _context.SnomedBodyRegions.CountAsync(e => e.UpdatedAt > since.Value, cancellationToken)
            : await _context.SnomedBodyRegions.CountAsync(cancellationToken);
        snomedCount += since.HasValue
            ? await _context.SnomedBodyStructures.CountAsync(e => e.UpdatedAt > since.Value, cancellationToken)
            : await _context.SnomedBodyStructures.CountAsync(cancellationToken);
        snomedCount += since.HasValue
            ? await _context.SnomedLateralities.CountAsync(e => e.UpdatedAt > since.Value, cancellationToken)
            : await _context.SnomedLateralities.CountAsync(cancellationToken);
        snomedCount += since.HasValue
            ? await _context.SnomedTopographicalModifiers.CountAsync(e => e.UpdatedAt > since.Value, cancellationToken)
            : await _context.SnomedTopographicalModifiers.CountAsync(cancellationToken);
        snomedCount += since.HasValue
            ? await _context.SnomedSeverityCodes.CountAsync(e => e.UpdatedAt > since.Value, cancellationToken)
            : await _context.SnomedSeverityCodes.CountAsync(cancellationToken);
        snomedCount += since.HasValue
            ? await _context.ClinicalConditions.CountAsync(e => e.UpdatedAt > since.Value, cancellationToken)
            : await _context.ClinicalConditions.CountAsync(cancellationToken);
        snomedCount += since.HasValue
            ? await _context.ClinicalEvents.CountAsync(e => e.UpdatedAt > since.Value, cancellationToken)
            : await _context.ClinicalEvents.CountAsync(cancellationToken);
        snomedCount += since.HasValue
            ? await _context.Medications.CountAsync(e => e.UpdatedAt > since.Value, cancellationToken)
            : await _context.Medications.CountAsync(cancellationToken);
        snomedCount += since.HasValue
            ? await _context.AllergyIntolerances.CountAsync(e => e.UpdatedAt > since.Value, cancellationToken)
            : await _context.AllergyIntolerances.CountAsync(cancellationToken);

        var volunteerCount = since.HasValue
            ? await _context.Volunteers.CountAsync(v => v.UpdatedAt > since.Value, cancellationToken)
            : await _context.Volunteers.CountAsync(cancellationToken);
        var volunteerLatest = await _context.Volunteers.MaxAsync(v => (DateTime?)v.UpdatedAt, cancellationToken);

        var researchCount = since.HasValue
            ? await _context.Research.CountAsync(r => r.UpdatedAt > since.Value, cancellationToken)
            : await _context.Research.CountAsync(cancellationToken);
        var researchLatest = await _context.Research.MaxAsync(r => (DateTime?)r.UpdatedAt, cancellationToken);

        var sessionCount = since.HasValue
            ? await _context.RecordSessions.CountAsync(s => s.UpdatedAt > since.Value, cancellationToken)
            : await _context.RecordSessions.CountAsync(cancellationToken);
        var sessionLatest = await _context.RecordSessions.MaxAsync(s => (DateTime?)s.UpdatedAt, cancellationToken);

        var recordingCount = await _context.RecordChannels
            .CountAsync(rc => !string.IsNullOrEmpty(rc.FileUrl), cancellationToken);

        return new SyncManifestResponse
        {
            NodeId = localNode?.Id.ToString() ?? string.Empty,
            NodeName = localNode?.NodeName ?? string.Empty,
            GeneratedAt = DateTime.UtcNow,
            LastSyncedAt = null,
            Snomed = new SyncEntitySummaryDto { Count = snomedCount },
            Volunteers = new SyncEntitySummaryDto { Count = volunteerCount, LatestUpdate = volunteerLatest },
            Research = new SyncEntitySummaryDto { Count = researchCount, LatestUpdate = researchLatest },
            Sessions = new SyncEntitySummaryDto { Count = sessionCount, LatestUpdate = sessionLatest },
            Recordings = new SyncRecordingSummaryDto { Count = recordingCount }
        };
    }

    public async Task<PagedSyncResult<object>> GetSnomedEntitiesAsync(
        string entityType, DateTime? since, int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var skip = (page - 1) * pageSize;

        // Each SNOMED entity type is handled individually since they have different schemas.
        // Results are cast to object for the generic PagedSyncResult<object> return type.
        switch (entityType)
        {
            case "body-regions":
            {
                var query = _context.SnomedBodyRegions.AsNoTracking();
                if (since.HasValue) query = query.Where(e => e.UpdatedAt > since.Value);
                query = query.OrderBy(e => e.UpdatedAt);
                var total = await query.CountAsync(cancellationToken);
                var items = await query.Skip(skip).Take(pageSize).ToListAsync(cancellationToken);
                return BuildResult<object>(items.Cast<object>().ToList(), page, pageSize, total);
            }
            case "body-structures":
            {
                var query = _context.SnomedBodyStructures.AsNoTracking();
                if (since.HasValue) query = query.Where(e => e.UpdatedAt > since.Value);
                query = query.OrderBy(e => e.UpdatedAt);
                var total = await query.CountAsync(cancellationToken);
                var items = await query.Skip(skip).Take(pageSize).ToListAsync(cancellationToken);
                return BuildResult<object>(items.Cast<object>().ToList(), page, pageSize, total);
            }
            case "topographical-modifiers":
            {
                var query = _context.SnomedTopographicalModifiers.AsNoTracking();
                if (since.HasValue) query = query.Where(e => e.UpdatedAt > since.Value);
                query = query.OrderBy(e => e.UpdatedAt);
                var total = await query.CountAsync(cancellationToken);
                var items = await query.Skip(skip).Take(pageSize).ToListAsync(cancellationToken);
                return BuildResult<object>(items.Cast<object>().ToList(), page, pageSize, total);
            }
            case "lateralities":
            {
                var query = _context.SnomedLateralities.AsNoTracking();
                if (since.HasValue) query = query.Where(e => e.UpdatedAt > since.Value);
                query = query.OrderBy(e => e.UpdatedAt);
                var total = await query.CountAsync(cancellationToken);
                var items = await query.Skip(skip).Take(pageSize).ToListAsync(cancellationToken);
                return BuildResult<object>(items.Cast<object>().ToList(), page, pageSize, total);
            }
            case "severity-codes":
            {
                var query = _context.SnomedSeverityCodes.AsNoTracking();
                if (since.HasValue) query = query.Where(e => e.UpdatedAt > since.Value);
                query = query.OrderBy(e => e.UpdatedAt);
                var total = await query.CountAsync(cancellationToken);
                var items = await query.Skip(skip).Take(pageSize).ToListAsync(cancellationToken);
                return BuildResult<object>(items.Cast<object>().ToList(), page, pageSize, total);
            }
            case "clinical-conditions":
            {
                var query = _context.ClinicalConditions.AsNoTracking();
                if (since.HasValue) query = query.Where(e => e.UpdatedAt > since.Value);
                query = query.OrderBy(e => e.UpdatedAt);
                var total = await query.CountAsync(cancellationToken);
                var items = await query.Skip(skip).Take(pageSize).ToListAsync(cancellationToken);
                return BuildResult<object>(items.Cast<object>().ToList(), page, pageSize, total);
            }
            case "clinical-events":
            {
                var query = _context.ClinicalEvents.AsNoTracking();
                if (since.HasValue) query = query.Where(e => e.UpdatedAt > since.Value);
                query = query.OrderBy(e => e.UpdatedAt);
                var total = await query.CountAsync(cancellationToken);
                var items = await query.Skip(skip).Take(pageSize).ToListAsync(cancellationToken);
                return BuildResult<object>(items.Cast<object>().ToList(), page, pageSize, total);
            }
            case "medications":
            {
                var query = _context.Medications.AsNoTracking();
                if (since.HasValue) query = query.Where(e => e.UpdatedAt > since.Value);
                query = query.OrderBy(e => e.UpdatedAt);
                var total = await query.CountAsync(cancellationToken);
                var items = await query.Skip(skip).Take(pageSize).ToListAsync(cancellationToken);
                return BuildResult<object>(items.Cast<object>().ToList(), page, pageSize, total);
            }
            case "allergy-intolerances":
            {
                var query = _context.AllergyIntolerances.AsNoTracking();
                if (since.HasValue) query = query.Where(e => e.UpdatedAt > since.Value);
                query = query.OrderBy(e => e.UpdatedAt);
                var total = await query.CountAsync(cancellationToken);
                var items = await query.Skip(skip).Take(pageSize).ToListAsync(cancellationToken);
                return BuildResult<object>(items.Cast<object>().ToList(), page, pageSize, total);
            }
            default:
                throw new ArgumentException($"Unknown SNOMED entity type: {entityType}");
        }
    }

    public async Task<PagedSyncResult<VolunteerEntity>> GetVolunteersAsync(
        DateTime? since, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var skip = (page - 1) * pageSize;
        var query = _context.Volunteers
            .AsNoTracking()
            .Include(v => v.VitalSigns)
            .Include(v => v.ClinicalConditions)
            .Include(v => v.Medications)
            .Include(v => v.AllergyIntolerances)
            .AsQueryable();

        if (since.HasValue) query = query.Where(v => v.UpdatedAt > since.Value);
        query = query.OrderBy(v => v.UpdatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(skip).Take(pageSize).ToListAsync(cancellationToken);
        return BuildResult(items, page, pageSize, total);
    }

    public async Task<PagedSyncResult<Domain.Entities.Research.Research>> GetResearchAsync(
        DateTime? since, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var skip = (page - 1) * pageSize;
        var query = _context.Research
            .AsNoTracking()
            .Include(r => r.Applications)
            .Include(r => r.ResearchDevices)
            .Include(r => r.ResearchResearchers)
                .ThenInclude(rr => rr.Researcher)
            .Include(r => r.ResearchVolunteers)
            .AsQueryable();

        if (since.HasValue) query = query.Where(r => r.UpdatedAt > since.Value);
        query = query.OrderBy(r => r.UpdatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(skip).Take(pageSize).ToListAsync(cancellationToken);
        return BuildResult(items, page, pageSize, total);
    }

    public async Task<PagedSyncResult<Domain.Entities.Record.RecordSession>> GetSessionsAsync(
        DateTime? since, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var skip = (page - 1) * pageSize;
        var query = _context.RecordSessions
            .AsNoTracking()
            .Include(s => s.Records)
                .ThenInclude(r => r.RecordChannels)
            .Include(s => s.SessionAnnotations)
            .AsQueryable();

        if (since.HasValue) query = query.Where(s => s.UpdatedAt > since.Value);
        query = query.OrderBy(s => s.UpdatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(skip).Take(pageSize).ToListAsync(cancellationToken);
        return BuildResult(items, page, pageSize, total);
    }

    public async Task<(byte[] data, string contentType, string fileName)?> GetRecordingFileAsync(
        Guid recordChannelId, CancellationToken cancellationToken = default)
    {
        var channel = await _context.RecordChannels
            .AsNoTracking()
            .FirstOrDefaultAsync(rc => rc.Id == recordChannelId, cancellationToken);

        if (channel == null || string.IsNullOrEmpty(channel.FileUrl))
        {
            return null;
        }

        try
        {
            var connectionString = _configuration["AzureBlobStorage:ConnectionString"] ?? "UseDevelopmentStorage=true";
            var blobClient = new BlobClient(new Uri(channel.FileUrl), null);

            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                _logger.LogWarning("Recording file not found in blob storage: {FileUrl}", channel.FileUrl);
                return null;
            }

            using var ms = new MemoryStream();
            await blobClient.DownloadToAsync(ms, cancellationToken);
            var fileName = Path.GetFileName(channel.FileUrl.Split('/').Last());
            return (ms.ToArray(), "application/octet-stream", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download recording file for channel {RecordChannelId}", recordChannelId);
            throw;
        }
    }

    private static PagedSyncResult<T> BuildResult<T>(List<T> items, int page, int pageSize, int total)
    {
        return new PagedSyncResult<T>
        {
            Data = items,
            Page = page,
            PageSize = pageSize,
            TotalRecords = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }

}
