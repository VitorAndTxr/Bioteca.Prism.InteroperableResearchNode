using Bioteca.Prism.Domain.Entities.Clinical;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bioteca.Prism.Data.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for AllergyIntolerance
/// </summary>
public class AllergyIntoleranceConfiguration : IEntityTypeConfiguration<AllergyIntolerance>
{
    public void Configure(EntityTypeBuilder<AllergyIntolerance> builder)
    {
        builder.ToTable("allergy_intolerances");

        // Primary key
        builder.HasKey(x => x.SnomedCode);

        builder.Property(x => x.SnomedCode)
            .HasColumnName("snomed_code")
            .HasMaxLength(50)
            .IsRequired();

        // Properties
        builder.Property(x => x.Category)
            .HasColumnName("category")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.SubstanceName)
            .HasColumnName("substance_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasColumnName("type")
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
            .HasDatabaseName("ix_allergy_intolerances_is_active");

        builder.HasIndex(x => x.Category)
            .HasDatabaseName("ix_allergy_intolerances_category");

        builder.HasIndex(x => x.Type)
            .HasDatabaseName("ix_allergy_intolerances_type");
    }
}
