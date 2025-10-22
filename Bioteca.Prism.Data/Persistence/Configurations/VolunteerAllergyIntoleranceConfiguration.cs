using Bioteca.Prism.Domain.Entities.Volunteer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bioteca.Prism.Data.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for VolunteerAllergyIntolerance
/// </summary>
public class VolunteerAllergyIntoleranceConfiguration : IEntityTypeConfiguration<VolunteerAllergyIntolerance>
{
    public void Configure(EntityTypeBuilder<VolunteerAllergyIntolerance> builder)
    {
        builder.ToTable("volunteer_allergy_intolerances");

        // Primary key
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        // Foreign keys
        builder.Property(x => x.VolunteerId)
            .HasColumnName("volunteer_id")
            .IsRequired();

        builder.Property(x => x.AllergyIntoleranceSnomedCode)
            .HasColumnName("allergy_intolerance_snomed_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.RecordedBy)
            .HasColumnName("recorded_by")
            .IsRequired();

        // Properties
        builder.Property(x => x.Criticality)
            .HasColumnName("criticality")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.ClinicalStatus)
            .HasColumnName("clinical_status")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Manifestations)
            .HasColumnName("manifestations")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.OnsetDate)
            .HasColumnName("onset_date")
            .IsRequired(false);

        builder.Property(x => x.LastOccurrence)
            .HasColumnName("last_occurrence")
            .IsRequired(false);

        builder.Property(x => x.VerificationStatus)
            .HasColumnName("verification_status")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Relationships
        builder.HasOne(x => x.Volunteer)
            .WithMany(x => x.AllergyIntolerances)
            .HasForeignKey(x => x.VolunteerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.AllergyIntolerance)
            .WithMany(x => x.VolunteerAllergyIntolerances)
            .HasForeignKey(x => x.AllergyIntoleranceSnomedCode)
            .OnDelete(DeleteBehavior.Restrict);


        // Indexes
        builder.HasIndex(x => x.VolunteerId)
            .HasDatabaseName("ix_volunteer_allergy_intolerances_volunteer_id");

        builder.HasIndex(x => x.AllergyIntoleranceSnomedCode)
            .HasDatabaseName("ix_volunteer_allergy_intolerances_snomed_code");

        builder.HasIndex(x => x.ClinicalStatus)
            .HasDatabaseName("ix_volunteer_allergy_intolerances_clinical_status");

        builder.HasIndex(x => x.RecordedBy)
            .HasDatabaseName("ix_volunteer_allergy_intolerances_recorded_by");
    }
}
