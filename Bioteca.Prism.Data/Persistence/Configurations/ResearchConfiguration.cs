using Bioteca.Prism.Domain.Entities.Research;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bioteca.Prism.Data.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for Research entity
/// </summary>
public class ResearchConfiguration : IEntityTypeConfiguration<Research>
{
    public void Configure(EntityTypeBuilder<Research> builder)
    {
        builder.ToTable("research");

        // Primary key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedOnAdd();

        // Foreign keys
        builder.Property(x => x.ResearchNodeId)
            .HasColumnName("research_node_id")
            .IsRequired();

        // Basic properties
        builder.Property(x => x.Title)
            .HasColumnName("title")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.StartDate)
            .HasColumnName("start_date")
            .IsRequired();

        builder.Property(x => x.EndDate)
            .HasColumnName("end_date");

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(50)
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
            .HasDatabaseName("ix_research_node_id");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("ix_research_status");

        builder.HasIndex(x => x.StartDate)
            .HasDatabaseName("ix_research_start_date");
    }
}
