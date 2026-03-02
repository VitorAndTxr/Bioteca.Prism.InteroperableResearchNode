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

    public override async Task<Domain.Entities.Volunteer.Volunteer?> GetByIdAsync(Guid id)
    {
        return await _dbSet
            .Include(v => v.ClinicalConditions)
            .Include(v => v.ClinicalEvents)
            .Include(v => v.Medications)
            .Include(v => v.AllergyIntolerances)
            .FirstOrDefaultAsync(v => v.VolunteerId == id);
    }

    public override async Task<List<Domain.Entities.Volunteer.Volunteer>> GetPagedAsync()
    {
        var page = _apiContext.PagingContext.RequestPaging.Page;
        var pageSize = _apiContext.PagingContext.RequestPaging.PageSize;

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var totalRecords = await _dbSet.CountAsync();

        _apiContext.PagingContext.ResponsePaging.TotalRecords = totalRecords;
        _apiContext.PagingContext.ResponsePaging.CurrentPage = page;
        _apiContext.PagingContext.ResponsePaging.PageSize = pageSize;

        return await _dbSet
            .Include(v => v.ClinicalConditions)
            .Include(v => v.ClinicalEvents)
            .Include(v => v.Medications)
            .Include(v => v.AllergyIntolerances)
            .OrderBy(v => v.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Domain.Entities.Volunteer.Volunteer?> GetByVolunteerCodeAsync(string volunteerCode, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(v => v.ClinicalConditions)
            .Include(v => v.ClinicalEvents)
            .Include(v => v.Medications)
            .Include(v => v.AllergyIntolerances)
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
