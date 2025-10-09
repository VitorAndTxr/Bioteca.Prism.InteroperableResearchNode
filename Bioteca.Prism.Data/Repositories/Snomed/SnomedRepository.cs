using Bioteca.Prism.Core.Database;
using Bioteca.Prism.Data.Interfaces.Snomed;
using Bioteca.Prism.Data.Persistence.Contexts;
using Bioteca.Prism.Domain.Entities.Snomed;
using Microsoft.EntityFrameworkCore;

namespace Bioteca.Prism.Data.Repositories.Snomed;

/// <summary>
/// Repository implementation for SNOMED CT laterality codes
/// </summary>
public class SnomedLateralityRepository : BaseRepository<SnomedLaterality, string>, ISnomedLateralityRepository
{
    public SnomedLateralityRepository(PrismDbContext context) : base(context)
    {
    }

    public async Task<List<SnomedLaterality>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(sl => sl.IsActive)
            .ToListAsync(cancellationToken);
    }
}

/// <summary>
/// Repository implementation for SNOMED CT topographical modifier codes
/// </summary>
public class SnomedTopographicalModifierRepository : BaseRepository<SnomedTopographicalModifier, string>, ISnomedTopographicalModifierRepository
{
    public SnomedTopographicalModifierRepository(PrismDbContext context) : base(context)
    {
    }

    public async Task<List<SnomedTopographicalModifier>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(stm => stm.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SnomedTopographicalModifier>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(stm => stm.Category == category && stm.IsActive)
            .ToListAsync(cancellationToken);
    }
}

/// <summary>
/// Repository implementation for SNOMED CT body region codes
/// </summary>
public class SnomedBodyRegionRepository : BaseRepository<SnomedBodyRegion, string>, ISnomedBodyRegionRepository
{
    public SnomedBodyRegionRepository(PrismDbContext context) : base(context)
    {
    }

    public async Task<List<SnomedBodyRegion>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(sbr => sbr.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SnomedBodyRegion>> GetSubRegionsAsync(string parentRegionCode, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(sbr => sbr.ParentRegionCode == parentRegionCode && sbr.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SnomedBodyRegion>> GetTopLevelRegionsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(sbr => sbr.ParentRegionCode == null && sbr.IsActive)
            .ToListAsync(cancellationToken);
    }
}

/// <summary>
/// Repository implementation for SNOMED CT body structure codes
/// </summary>
public class SnomedBodyStructureRepository : BaseRepository<SnomedBodyStructure, string>, ISnomedBodyStructureRepository
{
    public SnomedBodyStructureRepository(PrismDbContext context) : base(context)
    {
    }

    public async Task<List<SnomedBodyStructure>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(sbs => sbs.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SnomedBodyStructure>> GetByBodyRegionAsync(string bodyRegionCode, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(sbs => sbs.BodyRegionCode == bodyRegionCode && sbs.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SnomedBodyStructure>> GetSubStructuresAsync(string parentStructureCode, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(sbs => sbs.ParentStructureCode == parentStructureCode && sbs.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SnomedBodyStructure>> GetByStructureTypeAsync(string structureType, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(sbs => sbs.StructureType == structureType && sbs.IsActive)
            .ToListAsync(cancellationToken);
    }
}
