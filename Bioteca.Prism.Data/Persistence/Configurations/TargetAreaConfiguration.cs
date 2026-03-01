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
        builder.Property(x => x.RecordSessionId)
            .HasColumnName("record_session_id")
            .IsRequired();

        builder.Property(x => x.BodyStructureCode)
            .HasColumnName("body_structure_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.LateralityCode)
            .HasColumnName("laterality_code")
            .HasMaxLength(50);

        // Timestamps
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Relationships
        // Single authoritative declaration for the 1:1 session↔targetArea relationship.
        // RecordSession.TargetAreaId (SET NULL) is the optional FK; TargetArea.RecordSessionId (CASCADE) is the required FK.
        // WithOne(x => x.TargetArea) connects this side to RecordSession's navigation property so EF
        // treats both FKs as one bidirectional 1:1 relationship instead of creating two separate associations.
        builder.HasOne(x => x.RecordSession)
            .WithOne(x => x.TargetArea)
            .HasForeignKey<TargetArea>(x => x.RecordSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.BodyStructure)
            .WithMany()
            .HasForeignKey(x => x.BodyStructureCode)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Laterality)
            .WithMany(x => x.TargetAreas)
            .HasForeignKey(x => x.LateralityCode)
            .OnDelete(DeleteBehavior.Restrict);

        // N:M topographical modifiers via join entity
        builder.HasMany(x => x.TopographicalModifiers)
            .WithOne(x => x.TargetArea)
            .HasForeignKey(x => x.TargetAreaId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.RecordSessionId)
            .HasDatabaseName("ix_target_area_record_session_id");

        builder.HasIndex(x => x.BodyStructureCode)
            .HasDatabaseName("ix_target_area_body_structure_code");
    }
}
