using Bioteca.Prism.Domain.Entities.Record;

namespace Bioteca.Prism.Service.Services.Record;

/// <summary>
/// Service interface for record session operations
/// </summary>
public interface IRecordSessionService : IService<RecordSession, Guid>
{
    /// <summary>
    /// Get record sessions by research ID
    /// </summary>
    Task<List<RecordSession>> GetByResearchIdAsync(Guid researchId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get record sessions by volunteer ID
    /// </summary>
    Task<List<RecordSession>> GetByVolunteerIdAsync(Guid volunteerId, CancellationToken cancellationToken = default);
}
