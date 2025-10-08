using Bioteca.Prism.Data.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Bioteca.Prism.Data.Repositories.Volunteer;

/// <summary>
/// Repository implementation for volunteer persistence operations
/// </summary>
public class VolunteerRepository : Repository<Domain.Entities.Volunteer.Volunteer, Guid>, IVolunteerRepository
{
    public VolunteerRepository(PrismDbContext context) : base(context)
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
