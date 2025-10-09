using Bioteca.Prism.Domain.Entities.Clinical;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bioteca.Prism.Data.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for Medication
/// </summary>
public class MedicationConfiguration : IEntityTypeConfiguration<Medication>
{
    public void Configure(EntityTypeBuilder<Medication> builder)
    {
        builder.ToTable("medications");

        // Primary key
        builder.HasKey(x => x.SnomedCode);

        builder.Property(x => x.SnomedCode)
            .HasColumnName("snomed_code")
            .HasMaxLength(50)
            .IsRequired();

        // Properties
        builder.Property(x => x.MedicationName)
            .HasColumnName("medication_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ActiveIngredient)
            .HasColumnName("active_ingredient")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.AnvisaCode)
            .HasColumnName("anvisa_code")
            .HasMaxLength(50)
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
            .HasDatabaseName("ix_medications_is_active");

        builder.HasIndex(x => x.MedicationName)
            .HasDatabaseName("ix_medications_medication_name");

        builder.HasIndex(x => x.AnvisaCode)
            .HasDatabaseName("ix_medications_anvisa_code");
    }
}
