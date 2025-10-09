using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Domain.Entities.Snomed;

namespace Bioteca.Prism.Service.Interfaces.Snomed;

/// <summary>
/// Service interface for SNOMED CT laterality code operations
/// </summary>
public interface ISnomedLateralityService : IServiceBase<SnomedLaterality, string>
{
    /// <summary>
    /// Get active laterality codes
    /// </summary>
    Task<List<SnomedLaterality>> GetActiveAsync(CancellationToken cancellationToken = default);
}
