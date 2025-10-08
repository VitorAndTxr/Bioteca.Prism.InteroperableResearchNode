using Bioteca.Prism.Domain.Entities.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bioteca.Prism.Data.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for ResearchApplication join entity
/// </summary>
public class ResearchApplicationConfiguration : IEntityTypeConfiguration<ResearchApplication>
{
    public void Configure(EntityTypeBuilder<ResearchApplication> builder)
    {
        builder.ToTable("research_application");

        // Composite primary key
        builder.HasKey(x => new { x.ResearchId, x.ApplicationId });

        // Foreign keys
        builder.Property(x => x.ResearchId)
            .HasColumnName("research_id")
            .IsRequired();

        builder.Property(x => x.ApplicationId)
            .HasColumnName("application_id")
            .IsRequired();

        // Properties
        builder.Property(x => x.Role)
            .HasColumnName("role")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.AddedAt)
            .HasColumnName("added_at")
            .IsRequired();

        builder.Property(x => x.RemovedAt)
            .HasColumnName("removed_at")
            .IsRequired(false);

        builder.Property(x => x.Configuration)
            .HasColumnName("configuration")
            .HasColumnType("text")
            .IsRequired(false);

        // Relationships
        builder.HasOne(x => x.Research)
            .WithMany(x => x.ResearchApplications)
            .HasForeignKey(x => x.ResearchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Application)
            .WithMany(x => x.ResearchApplications)
            .HasForeignKey(x => x.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.ResearchId)
            .HasDatabaseName("ix_research_application_research_id");

        builder.HasIndex(x => x.ApplicationId)
            .HasDatabaseName("ix_research_application_application_id");
    }
}
