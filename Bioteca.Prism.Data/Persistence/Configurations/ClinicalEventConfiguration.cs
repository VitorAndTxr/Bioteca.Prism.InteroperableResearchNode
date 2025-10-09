using Bioteca.Prism.Domain.Entities.Clinical;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bioteca.Prism.Data.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for ClinicalEvent
/// </summary>
public class ClinicalEventConfiguration : IEntityTypeConfiguration<ClinicalEvent>
{
    public void Configure(EntityTypeBuilder<ClinicalEvent> builder)
    {
        builder.ToTable("clinical_events");

        // Primary key
        builder.HasKey(x => x.SnomedCode);

        builder.Property(x => x.SnomedCode)
            .HasColumnName("snomed_code")
            .HasMaxLength(50)
            .IsRequired();

        // Properties
        builder.Property(x => x.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Indexes
        builder.HasIndex(x => x.IsActive)
            .HasDatabaseName("ix_clinical_events_is_active");

        builder.HasIndex(x => x.DisplayName)
            .HasDatabaseName("ix_clinical_events_display_name");
    }
}
