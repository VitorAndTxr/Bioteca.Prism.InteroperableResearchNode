using Bioteca.Prism.Data.Repositories.Record;

namespace Bioteca.Prism.Service.Services.Record;

/// <summary>
/// Service implementation for record operations
/// </summary>
public class RecordService : Service<Domain.Entities.Record.Record, Guid>, IRecordService
{
    private readonly IRecordRepository _recordRepository;

    public RecordService(IRecordRepository repository) : base(repository)
    {
        _recordRepository = repository;
    }

    public async Task<List<Domain.Entities.Record.Record>> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _recordRepository.GetBySessionIdAsync(sessionId, cancellationToken);
    }

    public async Task<List<Domain.Entities.Record.Record>> GetByRecordTypeAsync(string recordType, CancellationToken cancellationToken = default)
    {
        return await _recordRepository.GetByRecordTypeAsync(recordType, cancellationToken);
    }
}
