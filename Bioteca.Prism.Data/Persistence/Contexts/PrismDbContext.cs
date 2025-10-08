using Bioteca.Prism.Domain.Entities.Application;
using Bioteca.Prism.Domain.Entities.Device;
using Bioteca.Prism.Domain.Entities.Node;
using Bioteca.Prism.Domain.Entities.Record;
using Bioteca.Prism.Domain.Entities.Research;
using Bioteca.Prism.Domain.Entities.Researcher;
using Bioteca.Prism.Domain.Entities.Sensor;
using Bioteca.Prism.Domain.Entities.Snomed;
using Bioteca.Prism.Domain.Entities.Volunteer;
using Microsoft.EntityFrameworkCore;

namespace Bioteca.Prism.Data.Persistence.Contexts;

/// <summary>
/// Main database context for PRISM node registry and research data
/// </summary>
public class PrismDbContext : DbContext
{
    public PrismDbContext(DbContextOptions<PrismDbContext> options) : base(options)
    {
    }

    // Node entities
    /// <summary>
    /// Research nodes in the network
    /// </summary>
    public DbSet<ResearchNode> ResearchNodes => Set<ResearchNode>();

    // Research entities
    /// <summary>
    /// Research projects
    /// </summary>
    public DbSet<Research> Research => Set<Research>();

    // Volunteer entities
    /// <summary>
    /// Volunteers participating in research
    /// </summary>
    public DbSet<Volunteer> Volunteers => Set<Volunteer>();

    // Researcher entities
    /// <summary>
    /// Researchers working on projects
    /// </summary>
    public DbSet<Researcher> Researchers => Set<Researcher>();

    // Application entities
    /// <summary>
    /// Applications used in research
    /// </summary>
    public DbSet<Application> Applications => Set<Application>();

    // Device entities
    /// <summary>
    /// Devices used in research
    /// </summary>
    public DbSet<Device> Devices => Set<Device>();

    // Sensor entities
    /// <summary>
    /// Sensors in devices
    /// </summary>
    public DbSet<Sensor> Sensors => Set<Sensor>();

    // Record entities
    /// <summary>
    /// Recording sessions
    /// </summary>
    public DbSet<RecordSession> RecordSessions => Set<RecordSession>();

    /// <summary>
    /// Data records
    /// </summary>
    public DbSet<Record> Records => Set<Record>();

    /// <summary>
    /// Record channels
    /// </summary>
    public DbSet<RecordChannel> RecordChannels => Set<RecordChannel>();

    /// <summary>
    /// Target areas (body structures)
    /// </summary>
    public DbSet<TargetArea> TargetAreas => Set<TargetArea>();

    // SNOMED CT entities
    /// <summary>
    /// SNOMED CT laterality codes
    /// </summary>
    public DbSet<SnomedLaterality> SnomedLateralities => Set<SnomedLaterality>();

    /// <summary>
    /// SNOMED CT topographical modifier codes
    /// </summary>
    public DbSet<SnomedTopographicalModifier> SnomedTopographicalModifiers => Set<SnomedTopographicalModifier>();

    /// <summary>
    /// SNOMED CT body region codes
    /// </summary>
    public DbSet<SnomedBodyRegion> SnomedBodyRegions => Set<SnomedBodyRegion>();

    /// <summary>
    /// SNOMED CT body structure codes
    /// </summary>
    public DbSet<SnomedBodyStructure> SnomedBodyStructures => Set<SnomedBodyStructure>();

    // Join tables
    /// <summary>
    /// Research-Application many-to-many relationship
    /// </summary>
    public DbSet<ResearchApplication> ResearchApplications => Set<ResearchApplication>();

    /// <summary>
    /// Research-Device many-to-many relationship
    /// </summary>
    public DbSet<ResearchDevice> ResearchDevices => Set<ResearchDevice>();

    /// <summary>
    /// Research-Volunteer many-to-many relationship
    /// </summary>
    public DbSet<ResearchVolunteer> ResearchVolunteers => Set<ResearchVolunteer>();

    /// <summary>
    /// Research-Researcher many-to-many relationship
    /// </summary>
    public DbSet<ResearchResearcher> ResearchResearchers => Set<ResearchResearcher>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PrismDbContext).Assembly);
    }
}
