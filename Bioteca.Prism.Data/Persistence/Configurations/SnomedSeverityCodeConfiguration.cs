using Bioteca.Prism.Domain.Entities.Snomed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bioteca.Prism.Data.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for SnomedSeverityCode
/// </summary>
public class SnomedSeverityCodeConfiguration : IEntityTypeConfiguration<SnomedSeverityCode>
{
    public void Configure(EntityTypeBuilder<SnomedSeverityCode> builder)
    {
        builder.ToTable("snomed_severity_codes");

        // Primary key
        builder.HasKey(x => x.Code);

        builder.Property(x => x.Code)
            .HasColumnName("code")
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
            .HasDatabaseName("ix_snomed_severity_codes_is_active");
    }
}
