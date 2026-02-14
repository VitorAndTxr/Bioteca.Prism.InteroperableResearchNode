using Bioteca.Prism.Domain.Entities.Record;
using Bioteca.Prism.Domain.Payloads.Record;
using RecordEntity = Bioteca.Prism.Domain.Entities.Record.Record;

namespace Bioteca.Prism.Service.Interfaces.Record;

/// <summary>
/// Service interface for clinical session operations
/// </summary>
public interface IClinicalSessionService
{
    Task<RecordSession> CreateAsync(CreateClinicalSessionPayload payload);
    Task<RecordSession?> GetByIdDetailAsync(Guid id);
    Task<List<RecordSession>> GetFilteredPagedAsync(Guid? researchId, Guid? volunteerId, string? status, DateTime? dateFrom, DateTime? dateTo);
    Task<RecordSession?> UpdateAsync(Guid id, UpdateClinicalSessionPayload payload);
    Task<RecordEntity> CreateRecordingAsync(Guid sessionId, CreateRecordingPayload payload);
    Task<List<RecordEntity>> GetRecordingsBySessionAsync(Guid sessionId);
    Task<SessionAnnotation> CreateAnnotationAsync(Guid sessionId, CreateAnnotationPayload payload);
    Task<List<SessionAnnotation>> GetAnnotationsBySessionAsync(Guid sessionId);
}
