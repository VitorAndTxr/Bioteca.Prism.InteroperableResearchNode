using Bioteca.Prism.Core.Interfaces;

namespace Bioteca.Prism.Data.Interfaces.Researcher;

public interface IResearchResearcherRepository : IBaseRepository<Domain.Entities.Research.ResearchResearcher, Guid>
{
    /// <summary>
    /// Get research-researcher associations by research ID
    /// </summary>
    Task<List<Domain.Entities.Research.ResearchResearcher>> GetByResearchIdAsync(Guid researchId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Get research-researcher associations by researcher ID
    /// </summary>
    Task<List<Domain.Entities.Research.ResearchResearcher>> GetByResearcherIdAsync(Guid researcherId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get research-researcher association by researcher ID and research ID
    /// </summary>
    Task<Domain.Entities.Research.ResearchResearcher?> GetByResearcherIdAndResearchIdAsync(Guid researcherId, Guid researchId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get researches associated with a given researcher ID
    /// </summary>
    Task<List<Domain.Entities.Research.Research>> GetResearchesFromResearcherIdAsync(Guid researcherId, CancellationToken cancellationToken = default);
}
