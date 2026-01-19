using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Service;
using Bioteca.Prism.Data.Interfaces.Clinical;
using Bioteca.Prism.Domain.DTOs.Snomed;
using Bioteca.Prism.Domain.Entities.Clinical;
using Bioteca.Prism.Service.Interfaces.Clinical;

namespace Bioteca.Prism.Service.Services.Clinical;

/// <summary>
/// Service implementation for clinical event operations
/// </summary>
public class ClinicalEventService : BaseService<ClinicalEvent, string>, IClinicalEventService
{
    private readonly IClinicalEventRepository _eventRepository;

    public ClinicalEventService(IClinicalEventRepository repository, IApiContext apiContext) : base(repository, apiContext)
    {
        _eventRepository = repository;
    }

    public async Task<List<SnomedClinicalEventDTO>> GetActiveEventsAsync()
    {
        var allEvents = await _eventRepository.GetAllAsync();
        return allEvents.Where(e => e.IsActive)
            .Select(evt => new SnomedClinicalEventDTO
            {
                SnomedCode = evt.SnomedCode,
                DisplayName = evt.DisplayName,
                Description = evt.Description
            }).ToList();
    }

    public async Task<List<ClinicalEvent>> SearchByNameAsync(string searchTerm)
    {
        var allEvents = await _eventRepository.GetAllAsync();
        return allEvents
            .Where(e => e.DisplayName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                       e.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<List<SnomedClinicalEventDTO>> GetAllClinicalEventsPaginateAsync()
    {
        var result = await _eventRepository.GetPagedAsync();

        return result.Where(x => x.IsActive)
            .Select(evt => new SnomedClinicalEventDTO
            {
                SnomedCode = evt.SnomedCode,
                DisplayName = evt.DisplayName,
                Description = evt.Description
            }).ToList();
    }

    public async Task<ClinicalEvent> AddAsync(SnomedClinicalEventDTO payload)
    {
        ValidateAddClinicalEventPayload(payload);

        ClinicalEvent newEvent = new ClinicalEvent
        {
            SnomedCode = payload.SnomedCode,
            DisplayName = payload.DisplayName,
            Description = payload.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return await _eventRepository.AddAsync(newEvent);
    }

    private void ValidateAddClinicalEventPayload(SnomedClinicalEventDTO payload)
    {
        if (string.IsNullOrWhiteSpace(payload.SnomedCode))
        {
            throw new ArgumentException("SnomedCode is required.");
        }

        if (string.IsNullOrWhiteSpace(payload.DisplayName))
        {
            throw new ArgumentException("DisplayName is required.");
        }

        if (_eventRepository.GetByIdAsync(payload.SnomedCode).Result != null)
        {
            throw new ArgumentException("A clinical event with the same SnomedCode already exists.");
        }
    }

    public async Task<SnomedClinicalEventDTO?> GetBySnomedCodeAsync(string snomedCode, CancellationToken cancellationToken = default)
    {
        var evt = await _eventRepository.GetByIdAsync(snomedCode);

        if (evt == null)
        {
            return null;
        }

        return new SnomedClinicalEventDTO
        {
            SnomedCode = evt.SnomedCode,
            DisplayName = evt.DisplayName,
            Description = evt.Description
        };
    }

    public async Task<SnomedClinicalEventDTO?> UpdateBySnomedCodeAsync(string snomedCode, UpdateSnomedClinicalEventDTO payload, CancellationToken cancellationToken = default)
    {
        var existingEvent = await _eventRepository.GetByIdAsync(snomedCode);

        if (existingEvent == null)
        {
            return null;
        }

        existingEvent.DisplayName = payload.DisplayName;
        existingEvent.Description = payload.Description;
        existingEvent.UpdatedAt = DateTime.UtcNow;

        await _eventRepository.UpdateAsync(existingEvent);

        return new SnomedClinicalEventDTO
        {
            SnomedCode = existingEvent.SnomedCode,
            DisplayName = existingEvent.DisplayName,
            Description = existingEvent.Description
        };
    }
}
