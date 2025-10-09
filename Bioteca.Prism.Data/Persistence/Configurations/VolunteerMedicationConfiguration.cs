using Bioteca.Prism.Domain.Entities.Volunteer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bioteca.Prism.Data.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for VolunteerMedication
/// </summary>
public class VolunteerMedicationConfiguration : IEntityTypeConfiguration<VolunteerMedication>
{
    public void Configure(EntityTypeBuilder<VolunteerMedication> builder)
    {
        builder.ToTable("volunteer_medications");

        // Primary key
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        // Foreign keys
        builder.Property(x => x.VolunteerId)
            .HasColumnName("volunteer_id")
            .IsRequired();

        builder.Property(x => x.MedicationSnomedCode)
            .HasColumnName("medication_snomed_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.ConditionId)
            .HasColumnName("condition_id")
            .IsRequired(false);

        builder.Property(x => x.RecordedBy)
            .HasColumnName("recorded_by")
            .IsRequired();

        // Properties
        builder.Property(x => x.Dosage)
            .HasColumnName("dosage")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Frequency)
            .HasColumnName("frequency")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Route)
            .HasColumnName("route")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.StartDate)
            .HasColumnName("start_date")
            .IsRequired();

        builder.Property(x => x.EndDate)
            .HasColumnName("end_date")
            .IsRequired(false);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasColumnName("notes")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Relationships
        builder.HasOne(x => x.Volunteer)
            .WithMany(x => x.Medications)
            .HasForeignKey(x => x.VolunteerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Medication)
            .WithMany(x => x.VolunteerMedications)
            .HasForeignKey(x => x.MedicationSnomedCode)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Condition)
            .WithMany(x => x.Medications)
            .HasForeignKey(x => x.ConditionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Recorder)
            .WithMany(x => x.PrescribedMedications)
            .HasForeignKey(x => x.RecordedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(x => x.VolunteerId)
            .HasDatabaseName("ix_volunteer_medications_volunteer_id");

        builder.HasIndex(x => x.MedicationSnomedCode)
            .HasDatabaseName("ix_volunteer_medications_medication_snomed_code");

        builder.HasIndex(x => x.ConditionId)
            .HasDatabaseName("ix_volunteer_medications_condition_id");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("ix_volunteer_medications_status");

        builder.HasIndex(x => x.RecordedBy)
            .HasDatabaseName("ix_volunteer_medications_recorded_by");
    }
}
