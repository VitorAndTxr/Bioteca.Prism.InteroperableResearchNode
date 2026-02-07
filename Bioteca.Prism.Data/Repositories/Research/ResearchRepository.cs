using Bioteca.Prism.Core.Database;
using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Data.Interfaces.Research;
using Bioteca.Prism.Data.Persistence.Contexts;
using Bioteca.Prism.Domain.Entities.Application;
using Bioteca.Prism.Domain.Entities.Device;
using Bioteca.Prism.Domain.Entities.Research;
using Bioteca.Prism.Domain.Entities.Sensor;
using Microsoft.EntityFrameworkCore;

namespace Bioteca.Prism.Data.Repositories.Research;

/// <summary>
/// Repository implementation for research persistence operations
/// </summary>
public class ResearchRepository : BaseRepository<Domain.Entities.Research.Research, Guid>, IResearchRepository
{
    private readonly PrismDbContext _prismContext;

    public ResearchRepository(PrismDbContext context, IApiContext apiContext) : base(context, apiContext)
    {
        _prismContext = context;
    }

    public async Task<List<Domain.Entities.Research.Research>> GetByNodeIdAsync(Guid nodeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.ResearchNodeId == nodeId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Domain.Entities.Research.Research>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.Status == status)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Domain.Entities.Research.Research>> GetActiveResearchAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.Status == "Active" && (r.EndDate == null || r.EndDate > DateTime.UtcNow))
            .ToListAsync(cancellationToken);
    }

    public override async Task<List<Domain.Entities.Research.Research>> GetPagedAsync()
    {
        var page = _apiContext.PagingContext.RequestPaging.Page;
        var pageSize = _apiContext.PagingContext.RequestPaging.PageSize;

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = _dbSet
            .Include(r => r.ResearchNode)
            .AsQueryable();

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        _apiContext.PagingContext.ResponsePaging.SetValues(page, pageSize, totalPages, totalCount);

        return items;
    }

    // Group 1: Core CRUD additions

    public async Task<Domain.Entities.Research.Research?> GetByIdWithCountsAsync(Guid id)
    {
        try
        {

            return await _dbSet
                .Include(r => r.ResearchNode)
                .Include(r => r.ResearchResearchers)
                .Include(r => r.ResearchVolunteers)
                .Include(r => r.Applications)
                .Include(r => r.ResearchDevices)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id);
        }catch(Exception ex)
        {
            throw;
        }
    }

    public async Task<List<Domain.Entities.Research.Research>> GetByStatusPagedAsync(string status)
    {
        var page = _apiContext.PagingContext.RequestPaging.Page;
        var pageSize = _apiContext.PagingContext.RequestPaging.PageSize;

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = _dbSet
            .Include(r => r.ResearchNode)
            .Where(r => r.Status == status);

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        _apiContext.PagingContext.ResponsePaging.SetValues(page, pageSize, totalPages, totalCount);

        return items;
    }

    public async Task<List<Domain.Entities.Research.Research>> GetActiveResearchPagedAsync()
    {
        var page = _apiContext.PagingContext.RequestPaging.Page;
        var pageSize = _apiContext.PagingContext.RequestPaging.PageSize;

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = _dbSet
            .Include(r => r.ResearchNode)
            .Where(r => r.Status == "Active" && (r.EndDate == null || r.EndDate > DateTime.UtcNow));

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        _apiContext.PagingContext.ResponsePaging.SetValues(page, pageSize, totalPages, totalCount);

        return items;
    }

    // Group 2: ResearchResearcher junction operations

    public async Task<List<ResearchResearcher>> GetResearchersByResearchIdAsync(Guid researchId)
    {
        var page = _apiContext.PagingContext.RequestPaging.Page;
        var pageSize = _apiContext.PagingContext.RequestPaging.PageSize;

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = _prismContext.Set<ResearchResearcher>()
            .Include(rr => rr.Researcher)
            .Where(rr => rr.ResearchId == researchId && rr.RemovedAt == null)
            .AsNoTracking();

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        _apiContext.PagingContext.ResponsePaging.SetValues(page, pageSize, totalPages, totalCount);

        return items;
    }

    public async Task<ResearchResearcher?> GetResearchResearcherAsync(Guid researchId, Guid researcherId)
    {
        return await _prismContext.Set<ResearchResearcher>()
            .Include(rr => rr.Researcher)
            .FirstOrDefaultAsync(rr => rr.ResearchId == researchId && rr.ResearcherId == researcherId && rr.RemovedAt == null);
    }

    public async Task<ResearchResearcher?> GetResearchResearcherIncludingRemovedAsync(Guid researchId, Guid researcherId)
    {
        return await _prismContext.Set<ResearchResearcher>()
            .Include(rr => rr.Researcher)
            .FirstOrDefaultAsync(rr => rr.ResearchId == researchId && rr.ResearcherId == researcherId);
    }

    public async Task<ResearchResearcher> AddResearchResearcherAsync(ResearchResearcher entity)
    {
        await _prismContext.Set<ResearchResearcher>().AddAsync(entity);
        await _prismContext.SaveChangesAsync();

        // Reload with navigation
        await _prismContext.Entry(entity).Reference(rr => rr.Researcher).LoadAsync();
        return entity;
    }

    public async Task<ResearchResearcher> UpdateResearchResearcherAsync(ResearchResearcher entity)
    {
        _prismContext.Set<ResearchResearcher>().Update(entity);
        await _prismContext.SaveChangesAsync();

        await _prismContext.Entry(entity).Reference(rr => rr.Researcher).LoadAsync();
        return entity;
    }

    public async Task<bool> RemoveResearchResearcherAsync(Guid researchId, Guid researcherId)
    {
        var entity = await _prismContext.Set<ResearchResearcher>()
            .FirstOrDefaultAsync(rr => rr.ResearchId == researchId && rr.ResearcherId == researcherId && rr.RemovedAt == null);

        if (entity == null) return false;

        entity.RemovedAt = DateTime.UtcNow;
        await _prismContext.SaveChangesAsync();
        return true;
    }

    // Group 3: ResearchVolunteer junction operations

    public async Task<List<ResearchVolunteer>> GetVolunteersByResearchIdAsync(Guid researchId)
    {
        var page = _apiContext.PagingContext.RequestPaging.Page;
        var pageSize = _apiContext.PagingContext.RequestPaging.PageSize;

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = _prismContext.Set<ResearchVolunteer>()
            .Include(rv => rv.Volunteer)
            .Where(rv => rv.ResearchId == researchId && rv.WithdrawnAt == null)
            .AsNoTracking();

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        _apiContext.PagingContext.ResponsePaging.SetValues(page, pageSize, totalPages, totalCount);

        return items;
    }

    public async Task<ResearchVolunteer?> GetResearchVolunteerAsync(Guid researchId, Guid volunteerId)
    {
        return await _prismContext.Set<ResearchVolunteer>()
            .Include(rv => rv.Volunteer)
            .FirstOrDefaultAsync(rv => rv.ResearchId == researchId && rv.VolunteerId == volunteerId && rv.WithdrawnAt == null);
    }

    public async Task<ResearchVolunteer?> GetResearchVolunteerIncludingWithdrawnAsync(Guid researchId, Guid volunteerId)
    {
        return await _prismContext.Set<ResearchVolunteer>()
            .Include(rv => rv.Volunteer)
            .FirstOrDefaultAsync(rv => rv.ResearchId == researchId && rv.VolunteerId == volunteerId);
    }

    public async Task<ResearchVolunteer> AddResearchVolunteerAsync(ResearchVolunteer entity)
    {
        await _prismContext.Set<ResearchVolunteer>().AddAsync(entity);
        await _prismContext.SaveChangesAsync();

        await _prismContext.Entry(entity).Reference(rv => rv.Volunteer).LoadAsync();
        return entity;
    }

    public async Task<ResearchVolunteer> UpdateResearchVolunteerAsync(ResearchVolunteer entity)
    {
        _prismContext.Set<ResearchVolunteer>().Update(entity);
        await _prismContext.SaveChangesAsync();

        await _prismContext.Entry(entity).Reference(rv => rv.Volunteer).LoadAsync();
        return entity;
    }

    public async Task<bool> RemoveResearchVolunteerAsync(Guid researchId, Guid volunteerId)
    {
        var entity = await _prismContext.Set<ResearchVolunteer>()
            .FirstOrDefaultAsync(rv => rv.ResearchId == researchId && rv.VolunteerId == volunteerId && rv.WithdrawnAt == null);

        if (entity == null) return false;

        entity.WithdrawnAt = DateTime.UtcNow;
        entity.EnrollmentStatus = "Withdrawn";
        await _prismContext.SaveChangesAsync();
        return true;
    }

    // Group 4: Application operations (scoped to research)

    public async Task<List<Domain.Entities.Application.Application>> GetApplicationsByResearchIdAsync(Guid researchId)
    {
        var page = _apiContext.PagingContext.RequestPaging.Page;
        var pageSize = _apiContext.PagingContext.RequestPaging.PageSize;

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = _prismContext.Set<Domain.Entities.Application.Application>()
            .Where(a => a.ResearchId == researchId)
            .AsNoTracking();

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        _apiContext.PagingContext.ResponsePaging.SetValues(page, pageSize, totalPages, totalCount);

        return items;
    }

    public async Task<Domain.Entities.Application.Application?> GetApplicationByIdAndResearchIdAsync(Guid applicationId, Guid researchId)
    {
        return await _prismContext.Set<Domain.Entities.Application.Application>()
            .FirstOrDefaultAsync(a => a.ApplicationId == applicationId && a.ResearchId == researchId);
    }

    public async Task<Domain.Entities.Application.Application> AddApplicationAsync(Domain.Entities.Application.Application entity)
    {
        await _prismContext.Set<Domain.Entities.Application.Application>().AddAsync(entity);
        await _prismContext.SaveChangesAsync();
        return entity;
    }

    public async Task<Domain.Entities.Application.Application> UpdateApplicationAsync(Domain.Entities.Application.Application entity)
    {
        _prismContext.Set<Domain.Entities.Application.Application>().Update(entity);
        await _prismContext.SaveChangesAsync();
        return entity;
    }

    public async Task<bool> DeleteApplicationAsync(Guid applicationId, Guid researchId)
    {
        var entity = await _prismContext.Set<Domain.Entities.Application.Application>()
            .FirstOrDefaultAsync(a => a.ApplicationId == applicationId && a.ResearchId == researchId);

        if (entity == null) return false;

        _prismContext.Set<Domain.Entities.Application.Application>().Remove(entity);
        await _prismContext.SaveChangesAsync();
        return true;
    }

    // Group 5: ResearchDevice junction operations

    public async Task<List<ResearchDevice>> GetDevicesByResearchIdAsync(Guid researchId)
    {
        var page = _apiContext.PagingContext.RequestPaging.Page;
        var pageSize = _apiContext.PagingContext.RequestPaging.PageSize;

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = _prismContext.Set<ResearchDevice>()
            .Include(rd => rd.Device)
                .ThenInclude(d => d.Sensors)
            .Where(rd => rd.ResearchId == researchId && rd.RemovedAt == null)
            .AsNoTracking();

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        _apiContext.PagingContext.ResponsePaging.SetValues(page, pageSize, totalPages, totalCount);

        return items;
    }

    public async Task<ResearchDevice?> GetResearchDeviceAsync(Guid researchId, Guid deviceId)
    {
        return await _prismContext.Set<ResearchDevice>()
            .Include(rd => rd.Device)
                .ThenInclude(d => d.Sensors)
            .FirstOrDefaultAsync(rd => rd.ResearchId == researchId && rd.DeviceId == deviceId && rd.RemovedAt == null);
    }

    public async Task<ResearchDevice?> GetResearchDeviceIncludingRemovedAsync(Guid researchId, Guid deviceId)
    {
        return await _prismContext.Set<ResearchDevice>()
            .Include(rd => rd.Device)
                .ThenInclude(d => d.Sensors)
            .FirstOrDefaultAsync(rd => rd.ResearchId == researchId && rd.DeviceId == deviceId);
    }

    public async Task<ResearchDevice> AddResearchDeviceAsync(ResearchDevice entity)
    {
        await _prismContext.Set<ResearchDevice>().AddAsync(entity);
        await _prismContext.SaveChangesAsync();

        await _prismContext.Entry(entity).Reference(rd => rd.Device).LoadAsync();
        await _prismContext.Entry(entity.Device).Collection(d => d.Sensors).LoadAsync();
        return entity;
    }

    public async Task<ResearchDevice> UpdateResearchDeviceAsync(ResearchDevice entity)
    {
        _prismContext.Set<ResearchDevice>().Update(entity);
        await _prismContext.SaveChangesAsync();

        await _prismContext.Entry(entity).Reference(rd => rd.Device).LoadAsync();
        await _prismContext.Entry(entity.Device).Collection(d => d.Sensors).LoadAsync();
        return entity;
    }

    public async Task<bool> RemoveResearchDeviceAsync(Guid researchId, Guid deviceId)
    {
        var entity = await _prismContext.Set<ResearchDevice>()
            .FirstOrDefaultAsync(rd => rd.ResearchId == researchId && rd.DeviceId == deviceId && rd.RemovedAt == null);

        if (entity == null) return false;

        entity.RemovedAt = DateTime.UtcNow;
        await _prismContext.SaveChangesAsync();
        return true;
    }

    // Group 6: Sensor read (scoped to research-device)

    public async Task<List<Domain.Entities.Sensor.Sensor>> GetSensorsByResearchDeviceAsync(Guid researchId, Guid deviceId)
    {
        // Pre-validate research-device assignment
        var assignment = await _prismContext.Set<ResearchDevice>()
            .AsNoTracking()
            .FirstOrDefaultAsync(rd => rd.ResearchId == researchId && rd.DeviceId == deviceId && rd.RemovedAt == null);

        if (assignment == null) return null!;

        return await _prismContext.Set<Domain.Entities.Sensor.Sensor>()
            .Where(s => s.DeviceId == deviceId)
            .AsNoTracking()
            .ToListAsync();
    }
}
