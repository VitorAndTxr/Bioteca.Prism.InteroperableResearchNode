using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Domain.DTOs.Device;
using Bioteca.Prism.Domain.Payloads.Device;

namespace Bioteca.Prism.Service.Interfaces.Device;

/// <summary>
/// Service interface for device operations
/// </summary>
public interface IDeviceService : IServiceBase<Domain.Entities.Device.Device, Guid>
{
    /// <summary>
    /// Get devices by research ID
    /// </summary>
    Task<List<Domain.Entities.Device.Device>> GetByResearchIdAsync(Guid researchId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get devices by manufacturer
    /// </summary>
    Task<List<Domain.Entities.Device.Device>> GetByManufacturerAsync(string manufacturer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all devices paginated, mapped to DTOs
    /// </summary>
    Task<List<DeviceDTO>> GetAllDevicesPaginateAsync();

    /// <summary>
    /// Create a new device from payload
    /// </summary>
    Task<DeviceDTO> AddDeviceAsync(AddDevicePayload payload);
}
