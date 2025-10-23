using Bioteca.Prism.Domain.Entities.Researcher;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bioteca.Prism.Data.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for Researcher entity
/// </summary>
public class ResearcherConfiguration : IEntityTypeConfiguration<Researcher>
{
    public void Configure(EntityTypeBuilder<Researcher> builder)
    {
        builder.ToTable("researcher");

        // Primary key
        builder.HasKey(x => x.ResearcherId);
        builder.Property(x => x.ResearcherId)
            .HasColumnName("researcher_id")
            .IsRequired()
            .ValueGeneratedOnAdd();

        // Foreign keys
        builder.Property(x => x.ResearchNodeId)
            .HasColumnName("research_node_id")
            .IsRequired();

        builder.Property(x => x.Orcid)
            .HasColumnName("orcid")
            .HasMaxLength(16)
            .IsRequired(true);

        // Basic properties
        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Email)
            .HasColumnName("email")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Institution)
            .HasColumnName("institution")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.Role)
            .HasColumnName("role")
            .HasMaxLength(100)
            .IsRequired();

        // Timestamps
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Relationships
        builder.HasOne(x => x.ResearchNode)
            .WithMany()
            .HasForeignKey(x => x.ResearchNodeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.ResearchNodeId)
            .HasDatabaseName("ix_researcher_research_node_id");

        builder.HasIndex(x => x.Email)
            .IsUnique()
            .HasDatabaseName("ix_researcher_email");
    }
}
