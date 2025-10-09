using Bioteca.Prism.Domain.Entities.Volunteer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bioteca.Prism.Data.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for VolunteerClinicalEvent
/// </summary>
public class VolunteerClinicalEventConfiguration : IEntityTypeConfiguration<VolunteerClinicalEvent>
{
    public void Configure(EntityTypeBuilder<VolunteerClinicalEvent> builder)
    {
        builder.ToTable("volunteer_clinical_events");

        // Primary key
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        // Foreign keys
        builder.Property(x => x.VolunteerId)
            .HasColumnName("volunteer_id")
            .IsRequired();

        builder.Property(x => x.SnomedCode)
            .HasColumnName("snomed_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.SeverityCode)
            .HasColumnName("severity_code")
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(x => x.TargetAreaId)
            .HasColumnName("target_area_id")
            .IsRequired(false);

        builder.Property(x => x.RecordSessionId)
            .HasColumnName("record_session_id")
            .IsRequired(false);

        builder.Property(x => x.RecordedBy)
            .HasColumnName("recorded_by")
            .IsRequired();

        // Properties
        builder.Property(x => x.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.EventDatetime)
            .HasColumnName("event_datetime")
            .IsRequired();

        builder.Property(x => x.DurationMinutes)
            .HasColumnName("duration_minutes")
            .IsRequired(false);

        builder.Property(x => x.NumericValue)
            .HasColumnName("numeric_value")
            .IsRequired(false);

        builder.Property(x => x.ValueUnit)
            .HasColumnName("value_unit")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Characteristics)
            .HasColumnName("characteristics")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Relationships
        builder.HasOne(x => x.Volunteer)
            .WithMany(x => x.ClinicalEvents)
            .HasForeignKey(x => x.VolunteerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ClinicalEvent)
            .WithMany(x => x.VolunteerClinicalEvents)
            .HasForeignKey(x => x.SnomedCode)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Severity)
            .WithMany(x => x.ClinicalEvents)
            .HasForeignKey(x => x.SeverityCode)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.TargetArea)
            .WithMany()
            .HasForeignKey(x => x.TargetAreaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.RecordSession)
            .WithMany(x => x.ClinicalEvents)
            .HasForeignKey(x => x.RecordSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Recorder)
            .WithMany(x => x.RecordedClinicalEvents)
            .HasForeignKey(x => x.RecordedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(x => x.VolunteerId)
            .HasDatabaseName("ix_volunteer_clinical_events_volunteer_id");

        builder.HasIndex(x => x.SnomedCode)
            .HasDatabaseName("ix_volunteer_clinical_events_snomed_code");

        builder.HasIndex(x => x.EventType)
            .HasDatabaseName("ix_volunteer_clinical_events_event_type");

        builder.HasIndex(x => x.EventDatetime)
            .HasDatabaseName("ix_volunteer_clinical_events_event_datetime");

        builder.HasIndex(x => x.SeverityCode)
            .HasDatabaseName("ix_volunteer_clinical_events_severity_code");

        builder.HasIndex(x => x.RecordSessionId)
            .HasDatabaseName("ix_volunteer_clinical_events_record_session_id");

        builder.HasIndex(x => x.RecordedBy)
            .HasDatabaseName("ix_volunteer_clinical_events_recorded_by");
    }
}
