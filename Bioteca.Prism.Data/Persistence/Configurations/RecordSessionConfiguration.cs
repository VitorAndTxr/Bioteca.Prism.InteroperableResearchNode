using Bioteca.Prism.Domain.Entities.Record;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bioteca.Prism.Data.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for RecordSession entity
/// </summary>
public class RecordSessionConfiguration : IEntityTypeConfiguration<RecordSession>
{
    public void Configure(EntityTypeBuilder<RecordSession> builder)
    {
        builder.ToTable("record_session");

        // Primary key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedOnAdd();

        // Foreign keys
        builder.Property(x => x.ResearchId)
            .HasColumnName("research_id");

        builder.Property(x => x.VolunteerId)
            .HasColumnName("volunteer_id")
            .IsRequired();

        builder.Property(x => x.TargetAreaId)
            .HasColumnName("target_area_id");

        // Basic properties
        builder.Property(x => x.StartAt)
            .HasColumnName("start_at")
            .IsRequired();

        builder.Property(x => x.FinishedAt)
            .HasColumnName("finished_at");

        // Timestamps
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Relationships
        builder.HasOne(x => x.Research)
            .WithMany(x => x.RecordSessions)
            .HasForeignKey(x => x.ResearchId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Volunteer)
            .WithMany(x => x.RecordSessions)
            .HasForeignKey(x => x.VolunteerId)
            .OnDelete(DeleteBehavior.Cascade);

        // The session↔targetArea 1:1 relationship is declared authoritatively in TargetAreaConfiguration.
        // The optional FK column (target_area_id) with SET NULL delete behaviour is defined there.

        // Indexes
        builder.HasIndex(x => x.ResearchId)
            .HasDatabaseName("ix_record_session_research_id");

        builder.HasIndex(x => x.VolunteerId)
            .HasDatabaseName("ix_record_session_volunteer_id");

        builder.HasIndex(x => x.StartAt)
            .HasDatabaseName("ix_record_session_start_at");

        builder.HasIndex(x => x.TargetAreaId)
            .HasDatabaseName("ix_record_session_target_area_id");
    }
}
