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

    public ClinicalEventService(IBaseRepository<ClinicalEvent, string> repository, IApiContext apiContext) : base(repository, apiContext)
    {
        _eventRepository = repository;
    }

    public async Task<List<ClinicalEvent>> GetActiveEventsAsync()
    {
        var allEvents = await _eventRepository.GetAllAsync();
        return allEvents.Where(e => e.IsActive).ToList();
    }

    public async Task<List<ClinicalEvent>> SearchByNameAsync(string searchTerm)
    {
        var allEvents = await _eventRepository.GetAllAsync();
        return allEvents
            .Where(e => e.DisplayName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                       e.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
