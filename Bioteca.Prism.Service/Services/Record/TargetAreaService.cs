using Bioteca.Prism.Core.Context;
using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Service;
using Bioteca.Prism.Data.Interfaces.Record;
using Bioteca.Prism.Domain.Entities.Record;
using Bioteca.Prism.Service.Interfaces.Record;

namespace Bioteca.Prism.Service.Services.Record;

/// <summary>
/// Service implementation for target area operations
/// </summary>
public class TargetAreaService : BaseService<TargetArea, Guid>, ITargetAreaService
{
    private readonly ITargetAreaRepository _targetAreaRepository;

    public TargetAreaService(ITargetAreaRepository repository, IApiContext apiContext) : base(repository, apiContext)
    {
        _targetAreaRepository = repository;
    }

    public async Task<List<TargetArea>> GetByRecordSessionIdAsync(Guid recordSessionId, CancellationToken cancellationToken = default)
    {
        return await _targetAreaRepository.GetByRecordSessionIdAsync(recordSessionId, cancellationToken);
    }

    public async Task<List<TargetArea>> GetByBodyStructureCodeAsync(string bodyStructureCode, CancellationToken cancellationToken = default)
    {
        return await _targetAreaRepository.GetByBodyStructureCodeAsync(bodyStructureCode, cancellationToken);
    }
}
