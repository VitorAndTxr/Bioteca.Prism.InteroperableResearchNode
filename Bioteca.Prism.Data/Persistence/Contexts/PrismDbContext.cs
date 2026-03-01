using Bioteca.Prism.Domain.Entities.Application;
using Bioteca.Prism.Domain.Entities.Clinical;
using Bioteca.Prism.Domain.Entities.Device;
using Bioteca.Prism.Domain.Entities.Node;
using Bioteca.Prism.Domain.Entities.Record;
using Bioteca.Prism.Domain.Entities.Research;
using Bioteca.Prism.Domain.Entities.Researcher;
using Bioteca.Prism.Domain.Entities.Sensor;
using Bioteca.Prism.Domain.Entities.Snomed;
using Bioteca.Prism.Domain.Entities.Sync;
using Bioteca.Prism.Domain.Entities.User;
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

    /// <summary>
    /// Topographical modifier join rows for target areas
    /// </summary>
    public DbSet<TargetAreaTopographicalModifier> TargetAreaTopographicalModifiers => Set<TargetAreaTopographicalModifier>();

    /// <summary>
    /// Session annotations
    /// </summary>
    public DbSet<SessionAnnotation> SessionAnnotations => Set<SessionAnnotation>();

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

    /// <summary>
    /// SNOMED CT severity codes
    /// </summary>
    public DbSet<SnomedSeverityCode> SnomedSeverityCodes => Set<SnomedSeverityCode>();

    // Clinical catalog entities
    /// <summary>
    /// Clinical condition catalog
    /// </summary>
    public DbSet<ClinicalCondition> ClinicalConditions => Set<ClinicalCondition>();

    /// <summary>
    /// Clinical event catalog
    /// </summary>
    public DbSet<ClinicalEvent> ClinicalEvents => Set<ClinicalEvent>();

    /// <summary>
    /// Medication catalog
    /// </summary>
    public DbSet<Medication> Medications => Set<Medication>();

    /// <summary>
    /// Allergy/intolerance catalog
    /// </summary>
    public DbSet<AllergyIntolerance> AllergyIntolerances => Set<AllergyIntolerance>();

    // Volunteer clinical data entities
    /// <summary>
    /// Volunteer vital signs measurements
    /// </summary>
    public DbSet<VitalSigns> VitalSigns => Set<VitalSigns>();

    /// <summary>
    /// Volunteer allergies and intolerances
    /// </summary>
    public DbSet<VolunteerAllergyIntolerance> VolunteerAllergyIntolerances => Set<VolunteerAllergyIntolerance>();

    /// <summary>
    /// Volunteer medications
    /// </summary>
    public DbSet<VolunteerMedication> VolunteerMedications => Set<VolunteerMedication>();

    /// <summary>
    /// Volunteer clinical conditions
    /// </summary>
    public DbSet<VolunteerClinicalCondition> VolunteerClinicalConditions => Set<VolunteerClinicalCondition>();

    /// <summary>
    /// Volunteer clinical events
    /// </summary>
    public DbSet<VolunteerClinicalEvent> VolunteerClinicalEvents => Set<VolunteerClinicalEvent>();

    // Join tables
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

    /// <summary>
    /// User accounts for system access
    /// </summary>
    public DbSet<User> Users => Set<User>();

    // Sync infrastructure
    /// <summary>
    /// Sync operation history per remote node
    /// </summary>
    public DbSet<SyncLog> SyncLogs => Set<SyncLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PrismDbContext).Assembly);
    }
}
