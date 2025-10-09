using Bioteca.Prism.Core.Service;
using Bioteca.Prism.Data.Interfaces.Record;
using Bioteca.Prism.Domain.Entities.Record;
using Bioteca.Prism.Service.Interfaces.Record;

namespace Bioteca.Prism.Service.Services.Record;

/// <summary>
/// Service implementation for record channel operations
/// </summary>
public class RecordChannelService : BaseService<RecordChannel, Guid>, IRecordChannelService
{
    private readonly IRecordChannelRepository _recordChannelRepository;

    public RecordChannelService(IRecordChannelRepository repository) : base(repository)
    {
        _recordChannelRepository = repository;
    }

    public async Task<List<RecordChannel>> GetByRecordIdAsync(Guid recordId, CancellationToken cancellationToken = default)
    {
        return await _recordChannelRepository.GetByRecordIdAsync(recordId, cancellationToken);
    }

    public async Task<List<RecordChannel>> GetBySensorIdAsync(Guid sensorId, CancellationToken cancellationToken = default)
    {
        return await _recordChannelRepository.GetBySensorIdAsync(sensorId, cancellationToken);
    }

    public async Task<List<RecordChannel>> GetBySignalTypeAsync(string signalType, CancellationToken cancellationToken = default)
    {
        return await _recordChannelRepository.GetBySignalTypeAsync(signalType, cancellationToken);
    }
}
