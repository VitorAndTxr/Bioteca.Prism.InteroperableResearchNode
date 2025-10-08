using Bioteca.Prism.Domain.Entities.Snomed;

namespace Bioteca.Prism.Service.Services.Snomed;

/// <summary>
/// Service interface for SNOMED CT topographical modifier code operations
/// </summary>
public interface ISnomedTopographicalModifierService : IService<SnomedTopographicalModifier, string>
{
    /// <summary>
    /// Get active topographical modifier codes
    /// </summary>
    Task<List<SnomedTopographicalModifier>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get modifiers by category
    /// </summary>
    Task<List<SnomedTopographicalModifier>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);
}
