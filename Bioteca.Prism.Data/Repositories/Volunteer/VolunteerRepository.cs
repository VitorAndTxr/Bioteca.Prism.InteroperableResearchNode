using Bioteca.Prism.Core.Database;
using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Data.Interfaces.Volunteer;
using Bioteca.Prism.Data.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Bioteca.Prism.Data.Repositories.Volunteer;

/// <summary>
/// Repository implementation for volunteer persistence operations
/// </summary>
public class VolunteerRepository : BaseRepository<Domain.Entities.Volunteer.Volunteer, Guid>, IVolunteerRepository
{
    public VolunteerRepository(PrismDbContext context, IApiContext apiContext) : base(context, apiContext)
    {
    }

    public async Task<Domain.Entities.Volunteer.Volunteer?> GetByVolunteerCodeAsync(string volunteerCode, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(v => v.VolunteerCode == volunteerCode, cancellationToken);
    }

    public async Task<List<Domain.Entities.Volunteer.Volunteer>> GetByNodeIdAsync(Guid nodeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(v => v.ResearchNodeId == nodeId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Domain.Entities.Volunteer.Volunteer>> GetByConsentStatusAsync(string consentStatus, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(v => v.ConsentStatus == consentStatus)
            .ToListAsync(cancellationToken);
    }
}
