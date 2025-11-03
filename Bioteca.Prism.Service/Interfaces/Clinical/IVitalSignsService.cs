using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Domain.Entities.Volunteer;

namespace Bioteca.Prism.Service.Interfaces.Clinical;

/// <summary>
/// Service interface for vital signs operations
/// </summary>
public interface IVitalSignsService : IServiceBase<VitalSigns, Guid>
{
    /// <summary>
    /// Get vital signs by volunteer ID
    /// </summary>
    Task<List<VitalSigns>> GetByVolunteerIdAsync(Guid volunteerId);

    /// <summary>
    /// Get vital signs by record session ID
    /// </summary>
    Task<List<VitalSigns>> GetByRecordSessionIdAsync(Guid recordSessionId);

    /// <summary>
    /// Get vital signs within date range
    /// </summary>
    Task<List<VitalSigns>> GetByDateRangeAsync(Guid volunteerId, DateTime startDate, DateTime endDate);
}
