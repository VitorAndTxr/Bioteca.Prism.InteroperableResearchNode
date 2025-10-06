using Bioteca.Prism.Domain.Entities.Node;
using Microsoft.EntityFrameworkCore;

namespace Bioteca.Prism.Data.Persistence.Contexts;

/// <summary>
/// Main database context for PRISM node registry
/// </summary>
public class PrismDbContext : DbContext
{
    public PrismDbContext(DbContextOptions<PrismDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Research nodes in the network
    /// </summary>
    public DbSet<ResearchNode> ResearchNodes => Set<ResearchNode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PrismDbContext).Assembly);
    }
}
