using Bioteca.Prism.Domain.Entities.Volunteer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bioteca.Prism.Data.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for VolunteerClinicalCondition
/// </summary>
public class VolunteerClinicalConditionConfiguration : IEntityTypeConfiguration<VolunteerClinicalCondition>
{
    public void Configure(EntityTypeBuilder<VolunteerClinicalCondition> builder)
    {
        builder.ToTable("volunteer_clinical_conditions");

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

        builder.Property(x => x.RecordedBy)
            .HasColumnName("recorded_by")
            .IsRequired();

        // Properties
        builder.Property(x => x.ClinicalStatus)
            .HasColumnName("clinical_status")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.OnsetDate)
            .HasColumnName("onset_date")
            .IsRequired(false);

        builder.Property(x => x.AbatementDate)
            .HasColumnName("abatement_date")
            .IsRequired(false);

        builder.Property(x => x.VerificationStatus)
            .HasColumnName("verification_status")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.ClinicalNotes)
            .HasColumnName("clinical_notes")
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
            .WithMany(x => x.ClinicalConditions)
            .HasForeignKey(x => x.VolunteerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ClinicalCondition)
            .WithMany(x => x.VolunteerClinicalConditions)
            .HasForeignKey(x => x.SnomedCode)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Severity)
            .WithMany(x => x.ClinicalConditions)
            .HasForeignKey(x => x.SeverityCode)
            .OnDelete(DeleteBehavior.Restrict);


        // Indexes
        builder.HasIndex(x => x.VolunteerId)
            .HasDatabaseName("ix_volunteer_clinical_conditions_volunteer_id");

        builder.HasIndex(x => x.SnomedCode)
            .HasDatabaseName("ix_volunteer_clinical_conditions_snomed_code");

        builder.HasIndex(x => x.ClinicalStatus)
            .HasDatabaseName("ix_volunteer_clinical_conditions_clinical_status");

        builder.HasIndex(x => x.SeverityCode)
            .HasDatabaseName("ix_volunteer_clinical_conditions_severity_code");

        builder.HasIndex(x => x.RecordedBy)
            .HasDatabaseName("ix_volunteer_clinical_conditions_recorded_by");
    }
}
