using Bioteca.Prism.Domain.Entities.Snomed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bioteca.Prism.Data.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for SnomedTopographicalModifier entity
/// </summary>
public class SnomedTopographicalModifierConfiguration : IEntityTypeConfiguration<SnomedTopographicalModifier>
{
    public void Configure(EntityTypeBuilder<SnomedTopographicalModifier> builder)
    {
        builder.ToTable("snomed_topographical_modifier");

        // Primary key
        builder.HasKey(x => x.Code);
        builder.Property(x => x.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();

        // Basic properties
        builder.Property(x => x.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Category)
            .HasColumnName("category")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        // Indexes
        builder.HasIndex(x => x.Category)
            .HasDatabaseName("ix_snomed_topographical_modifier_category");

        builder.HasIndex(x => x.IsActive)
            .HasDatabaseName("ix_snomed_topographical_modifier_is_active");
    }
}
