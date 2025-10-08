using Bioteca.Prism.Domain.Entities.Research;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bioteca.Prism.Data.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for ResearchResearcher join table
/// </summary>
public class ResearchResearcherConfiguration : IEntityTypeConfiguration<ResearchResearcher>
{
    public void Configure(EntityTypeBuilder<ResearchResearcher> builder)
    {
        builder.ToTable("research_researcher");

        // Composite primary key
        builder.HasKey(x => new { x.ResearchId, x.ResearcherId });

        // Foreign keys
        builder.Property(x => x.ResearchId)
            .HasColumnName("research_id")
            .IsRequired();

        builder.Property(x => x.ResearcherId)
            .HasColumnName("researcher_id")
            .IsRequired();

        // Basic properties
        builder.Property(x => x.IsPrincipal)
            .HasColumnName("is_principal")
            .IsRequired();

        builder.Property(x => x.AssignedAt)
            .HasColumnName("assigned_at")
            .IsRequired();

        builder.Property(x => x.RemovedAt)
            .HasColumnName("removed_at");

        // Relationships
        builder.HasOne(x => x.Research)
            .WithMany(x => x.ResearchResearchers)
            .HasForeignKey(x => x.ResearchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Researcher)
            .WithMany(x => x.ResearchResearchers)
            .HasForeignKey(x => x.ResearcherId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.IsPrincipal)
            .HasDatabaseName("ix_research_researcher_is_principal");

        builder.HasIndex(x => x.AssignedAt)
            .HasDatabaseName("ix_research_researcher_assigned_at");
    }
}
