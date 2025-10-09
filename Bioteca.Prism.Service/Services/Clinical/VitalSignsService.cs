using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Service;
using Bioteca.Prism.Domain.Entities.Volunteer;
using Bioteca.Prism.Service.Interfaces.Clinical;

namespace Bioteca.Prism.Service.Services.Clinical;

/// <summary>
/// Service implementation for vital signs operations
/// </summary>
public class VitalSignsService : BaseService<VitalSigns, Guid>, IVitalSignsService
{
    private readonly IBaseRepository<VitalSigns, Guid> _vitalSignsRepository;

    public VitalSignsService(IBaseRepository<VitalSigns, Guid> repository) : base(repository)
    {
        _vitalSignsRepository = repository;
    }

    public async Task<List<VitalSigns>> GetByVolunteerIdAsync(Guid volunteerId, CancellationToken cancellationToken = default)
    {
        var allVitalSigns = await _vitalSignsRepository.GetAllAsync(cancellationToken);
        return allVitalSigns
            .Where(v => v.VolunteerId == volunteerId)
            .OrderByDescending(v => v.MeasurementDatetime)
            .ToList();
    }

    public async Task<List<VitalSigns>> GetByRecordSessionIdAsync(Guid recordSessionId, CancellationToken cancellationToken = default)
    {
        var allVitalSigns = await _vitalSignsRepository.GetAllAsync(cancellationToken);
        return allVitalSigns
            .Where(v => v.RecordSessionId == recordSessionId)
            .OrderByDescending(v => v.MeasurementDatetime)
            .ToList();
    }

    public async Task<List<VitalSigns>> GetByDateRangeAsync(Guid volunteerId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var allVitalSigns = await _vitalSignsRepository.GetAllAsync(cancellationToken);
        return allVitalSigns
            .Where(v => v.VolunteerId == volunteerId &&
                       v.MeasurementDatetime >= startDate &&
                       v.MeasurementDatetime <= endDate)
            .OrderByDescending(v => v.MeasurementDatetime)
            .ToList();
    }
}
