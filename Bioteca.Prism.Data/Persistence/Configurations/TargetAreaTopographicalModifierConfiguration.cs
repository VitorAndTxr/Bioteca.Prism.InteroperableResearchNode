using Bioteca.Prism.Domain.Entities.Record;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bioteca.Prism.Data.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for the TargetArea ↔ SnomedTopographicalModifier join entity
/// </summary>
public class TargetAreaTopographicalModifierConfiguration : IEntityTypeConfiguration<TargetAreaTopographicalModifier>
{
    public void Configure(EntityTypeBuilder<TargetAreaTopographicalModifier> builder)
    {
        builder.ToTable("target_area_topographical_modifier");

        builder.HasKey(x => new { x.TargetAreaId, x.TopographicalModifierCode });

        builder.Property(x => x.TargetAreaId)
            .HasColumnName("target_area_id")
            .IsRequired();

        builder.Property(x => x.TopographicalModifierCode)
            .HasColumnName("topographical_modifier_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasOne(x => x.TargetArea)
            .WithMany(x => x.TopographicalModifiers)
            .HasForeignKey(x => x.TargetAreaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.TopographicalModifier)
            .WithMany(x => x.TargetAreaTopographicalModifiers)
            .HasForeignKey(x => x.TopographicalModifierCode)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
