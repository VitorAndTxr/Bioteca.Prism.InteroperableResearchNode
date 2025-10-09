using Bioteca.Prism.Core.Interfaces;

namespace Bioteca.Prism.Data.Interfaces.Device;

/// <summary>
/// Repository interface for device persistence operations
/// </summary>
public interface IDeviceRepository : IBaseRepository<Domain.Entities.Device.Device, Guid>
{
    /// <summary>
    /// Get devices by research ID
    /// </summary>
    Task<List<Domain.Entities.Device.Device>> GetByResearchIdAsync(Guid researchId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get devices by manufacturer
    /// </summary>
    Task<List<Domain.Entities.Device.Device>> GetByManufacturerAsync(string manufacturer, CancellationToken cancellationToken = default);
}
