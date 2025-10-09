using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Service;
using Bioteca.Prism.Domain.Entities.Clinical;
using Bioteca.Prism.Service.Interfaces.Clinical;

namespace Bioteca.Prism.Service.Services.Clinical;

/// <summary>
/// Service implementation for clinical event operations
/// </summary>
public class ClinicalEventService : BaseService<ClinicalEvent, string>, IClinicalEventService
{
    private readonly IBaseRepository<ClinicalEvent, string> _eventRepository;

    public ClinicalEventService(IBaseRepository<ClinicalEvent, string> repository) : base(repository)
    {
        _eventRepository = repository;
    }

    public async Task<List<ClinicalEvent>> GetActiveEventsAsync(CancellationToken cancellationToken = default)
    {
        var allEvents = await _eventRepository.GetAllAsync(cancellationToken);
        return allEvents.Where(e => e.IsActive).ToList();
    }

    public async Task<List<ClinicalEvent>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var allEvents = await _eventRepository.GetAllAsync(cancellationToken);
        return allEvents
            .Where(e => e.DisplayName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                       e.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
