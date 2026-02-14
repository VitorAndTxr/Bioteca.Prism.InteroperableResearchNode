using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Data.Interfaces.Record;
using Bioteca.Prism.Domain.Entities.Record;
using Bioteca.Prism.Domain.Payloads.Record;
using Bioteca.Prism.Service.Interfaces.Record;
using RecordEntity = Bioteca.Prism.Domain.Entities.Record.Record;

namespace Bioteca.Prism.Service.Services.Record;

/// <summary>
/// Service implementation for clinical session operations
/// </summary>
public class ClinicalSessionService : IClinicalSessionService
{
    private readonly IRecordSessionRepository _recordSessionRepository;
    private readonly IRecordRepository _recordRepository;
    private readonly IRecordChannelRepository _recordChannelRepository;
    private readonly ISessionAnnotationRepository _sessionAnnotationRepository;
    private readonly IApiContext _apiContext;

    public ClinicalSessionService(
        IRecordSessionRepository recordSessionRepository,
        IRecordRepository recordRepository,
        IRecordChannelRepository recordChannelRepository,
        ISessionAnnotationRepository sessionAnnotationRepository,
        IApiContext apiContext)
    {
        _recordSessionRepository = recordSessionRepository;
        _recordRepository = recordRepository;
        _recordChannelRepository = recordChannelRepository;
        _sessionAnnotationRepository = sessionAnnotationRepository;
        _apiContext = apiContext;
    }

    public async Task<RecordSession> CreateAsync(CreateClinicalSessionPayload payload)
    {
        // Upsert: if session with Id exists, update; otherwise create new
        var existing = await _recordSessionRepository.GetByIdAsync(payload.Id);

        if (existing != null)
        {
            existing.ResearchId = payload.ResearchId;
            existing.VolunteerId = payload.VolunteerId;
            existing.ClinicalContext = payload.ClinicalContext;
            existing.StartAt = payload.StartAt;
            existing.FinishedAt = payload.FinishedAt;
            existing.UpdatedAt = DateTime.UtcNow;
            return await _recordSessionRepository.UpdateAsync(existing);
        }

        var session = new RecordSession
        {
            Id = payload.Id,
            ResearchId = payload.ResearchId,
            VolunteerId = payload.VolunteerId,
            ClinicalContext = payload.ClinicalContext,
            StartAt = payload.StartAt,
            FinishedAt = payload.FinishedAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return await _recordSessionRepository.AddAsync(session);
    }

    public async Task<RecordSession?> GetByIdDetailAsync(Guid id)
    {
        return await _recordSessionRepository.GetByIdWithDetailsAsync(id);
    }

    public async Task<List<RecordSession>> GetFilteredPagedAsync(
        Guid? researchId,
        Guid? volunteerId,
        string? status,
        DateTime? dateFrom,
        DateTime? dateTo)
    {
        bool? isCompleted = status?.ToLowerInvariant() switch
        {
            "active" => false,
            "completed" => true,
            null => null,
            _ => throw new ArgumentException($"Invalid status '{status}'. Must be 'active' or 'completed'.")
        };

        return await _recordSessionRepository.GetFilteredPagedAsync(
            researchId, volunteerId, isCompleted, dateFrom, dateTo);
    }

    public async Task<RecordSession?> UpdateAsync(Guid id, UpdateClinicalSessionPayload payload)
    {
        var session = await _recordSessionRepository.GetByIdAsync(id);
        if (session == null) return null;

        if (payload.FinishedAt.HasValue)
            session.FinishedAt = payload.FinishedAt.Value;

        if (payload.ClinicalContext != null)
            session.ClinicalContext = payload.ClinicalContext;

        session.UpdatedAt = DateTime.UtcNow;
        return await _recordSessionRepository.UpdateAsync(session);
    }

    public async Task<RecordEntity> CreateRecordingAsync(Guid sessionId, CreateRecordingPayload payload)
    {
        var session = await _recordSessionRepository.GetByIdAsync(sessionId);
        if (session == null)
            throw new KeyNotFoundException($"Session with ID {sessionId} not found");

        var record = new RecordEntity
        {
            Id = payload.Id,
            RecordSessionId = sessionId,
            CollectionDate = payload.CollectionDate,
            SessionId = sessionId.ToString(),
            RecordType = payload.SignalType,
            Notes = string.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _recordRepository.AddAsync(record);

        var channel = new RecordChannel
        {
            Id = Guid.NewGuid(),
            RecordId = record.Id,
            SensorId = payload.SensorId,
            SignalType = payload.SignalType,
            FileUrl = payload.FileUrl,
            SamplingRate = payload.SamplingRate,
            SamplesCount = payload.SamplesCount,
            StartTimestamp = payload.CollectionDate,
            CreatedAt = DateTime.UtcNow
        };

        await _recordChannelRepository.AddAsync(channel);

        return record;
    }

    public async Task<List<RecordEntity>> GetRecordingsBySessionAsync(Guid sessionId)
    {
        return await _recordRepository.GetBySessionIdAsync(sessionId);
    }

    public async Task<SessionAnnotation> CreateAnnotationAsync(Guid sessionId, CreateAnnotationPayload payload)
    {
        var session = await _recordSessionRepository.GetByIdAsync(sessionId);
        if (session == null)
            throw new KeyNotFoundException($"Session with ID {sessionId} not found");

        var annotation = new SessionAnnotation
        {
            Id = payload.Id,
            RecordSessionId = sessionId,
            Text = payload.Text,
            CreatedAt = payload.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };

        return await _sessionAnnotationRepository.AddAsync(annotation);
    }

    public async Task<List<SessionAnnotation>> GetAnnotationsBySessionAsync(Guid sessionId)
    {
        return await _sessionAnnotationRepository.GetBySessionIdAsync(sessionId);
    }
}
