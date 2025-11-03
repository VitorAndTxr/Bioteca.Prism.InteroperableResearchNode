using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Service;
using Bioteca.Prism.Data.Interfaces.Record;
using Bioteca.Prism.Domain.Entities.Record;
using Bioteca.Prism.Service.Interfaces.Record;

namespace Bioteca.Prism.Service.Services.Record;

/// <summary>
/// Service implementation for record session operations
/// </summary>
public class RecordSessionService : BaseService<RecordSession, Guid>, IRecordSessionService
{
    private readonly IRecordSessionRepository _recordSessionRepository;

    public RecordSessionService(IRecordSessionRepository repository, IApiContext apiContext) : base(repository, apiContext)
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
