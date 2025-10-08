using Bioteca.Prism.Data.Repositories.Record;
using Bioteca.Prism.Domain.Entities.Record;

namespace Bioteca.Prism.Service.Services.Record;

/// <summary>
/// Service implementation for record session operations
/// </summary>
public class RecordSessionService : Service<RecordSession, Guid>, IRecordSessionService
{
    private readonly IRecordSessionRepository _recordSessionRepository;

    public RecordSessionService(IRecordSessionRepository repository) : base(repository)
    {
        _recordSessionRepository = repository;
    }

    public async Task<List<RecordSession>> GetByResearchIdAsync(Guid researchId, CancellationToken cancellationToken = default)
    {
        return await _recordSessionRepository.GetByResearchIdAsync(researchId, cancellationToken);
    }

    public async Task<List<RecordSession>> GetByVolunteerIdAsync(Guid volunteerId, CancellationToken cancellationToken = default)
    {
        return await _recordSessionRepository.GetByVolunteerIdAsync(volunteerId, cancellationToken);
    }
}
