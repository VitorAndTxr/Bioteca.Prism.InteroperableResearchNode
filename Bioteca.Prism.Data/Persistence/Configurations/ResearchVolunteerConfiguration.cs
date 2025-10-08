using Bioteca.Prism.Domain.Entities.Research;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bioteca.Prism.Data.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for ResearchVolunteer join table
/// </summary>
public class ResearchVolunteerConfiguration : IEntityTypeConfiguration<ResearchVolunteer>
{
    public void Configure(EntityTypeBuilder<ResearchVolunteer> builder)
    {
        builder.ToTable("research_volunteer");

        // Composite primary key
        builder.HasKey(x => new { x.ResearchId, x.VolunteerId });

        // Foreign keys
        builder.Property(x => x.ResearchId)
            .HasColumnName("research_id")
            .IsRequired();

        builder.Property(x => x.VolunteerId)
            .HasColumnName("volunteer_id")
            .IsRequired();

        // Basic properties
        builder.Property(x => x.EnrollmentStatus)
            .HasColumnName("enrollment_status")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.ConsentDate)
            .HasColumnName("consent_date")
            .IsRequired();

        builder.Property(x => x.ConsentVersion)
            .HasColumnName("consent_version")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.ExclusionReason)
            .HasColumnName("exclusion_reason")
            .HasColumnType("text");

        builder.Property(x => x.EnrolledAt)
            .HasColumnName("enrolled_at")
            .IsRequired();

        builder.Property(x => x.WithdrawnAt)
            .HasColumnName("withdrawn_at");

        // Relationships
        builder.HasOne(x => x.Research)
            .WithMany(x => x.ResearchVolunteers)
            .HasForeignKey(x => x.ResearchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Volunteer)
            .WithMany(x => x.ResearchVolunteers)
            .HasForeignKey(x => x.VolunteerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.EnrollmentStatus)
            .HasDatabaseName("ix_research_volunteer_enrollment_status");

        builder.HasIndex(x => x.EnrolledAt)
            .HasDatabaseName("ix_research_volunteer_enrolled_at");
    }
}
