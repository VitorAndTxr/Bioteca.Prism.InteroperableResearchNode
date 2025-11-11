using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Domain.DTOs.Snomed;
using Bioteca.Prism.Domain.Entities.Snomed;

namespace Bioteca.Prism.Service.Interfaces.Snomed;

/// <summary>
/// Service interface for SNOMED CT topographical modifier code operations
/// </summary>
public interface ISnomedTopographicalModifierService : IServiceBase<SnomedTopographicalModifier, string>
{
    /// <summary>
    /// Get active topographical modifier codes
    /// </summary>
    Task<List<SnomedTopographicalModifierDTO>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get modifiers by category
    /// </summary>
    Task<List<SnomedTopographicalModifier>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all topographical modifiers with pagination
    /// </summary>
    Task<List<SnomedTopographicalModifierDTO>> GetAllTopographicalModifiersPaginateAsync();

    /// <summary>
    /// Add a new topographical modifier
    /// </summary>
    Task<SnomedTopographicalModifier> AddAsync(SnomedTopographicalModifierDTO payload);
}
