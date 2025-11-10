using Bioteca.Prism.Domain.Entities.Record;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bioteca.Prism.Data.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for TargetArea entity
/// </summary>
public class TargetAreaConfiguration : IEntityTypeConfiguration<TargetArea>
{
    public void Configure(EntityTypeBuilder<TargetArea> builder)
    {
        builder.ToTable("target_area");

        // Primary key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedOnAdd();

        // Foreign keys
        builder.Property(x => x.RecordChannelId)
            .HasColumnName("record_channel_id")
            .IsRequired();

        builder.Property(x => x.BodyStructureCode)
            .HasColumnName("body_structure_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.LateralityCode)
            .HasColumnName("laterality_code")
            .HasMaxLength(50);

        builder.Property(x => x.TopographicalModifierCode)
            .HasColumnName("topographical_modifier_code")
            .HasMaxLength(50);

        // Basic properties
        builder.Property(x => x.Notes)
            .HasColumnName("notes")
            .HasColumnType("text")
            .IsRequired();

        // Timestamps
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Relationships
        builder.HasOne(x => x.RecordChannel)
            .WithMany(x => x.TargetAreas)
            .HasForeignKey(x => x.RecordChannelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Laterality)
            .WithMany(x => x.TargetAreas)
            .HasForeignKey(x => x.LateralityCode)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.TopographicalModifier)
            .WithMany(x => x.TargetAreas)
            .HasForeignKey(x => x.TopographicalModifierCode)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(x => x.RecordChannelId)
            .HasDatabaseName("ix_target_area_record_channel_id");

        builder.HasIndex(x => x.BodyStructureCode)
            .HasDatabaseName("ix_target_area_body_structure_code");
    }
}
