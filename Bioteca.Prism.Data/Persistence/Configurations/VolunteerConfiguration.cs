using Bioteca.Prism.Domain.Entities.Volunteer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bioteca.Prism.Data.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for Volunteer entity
/// </summary>
public class VolunteerConfiguration : IEntityTypeConfiguration<Volunteer>
{
    public void Configure(EntityTypeBuilder<Volunteer> builder)
    {
        builder.ToTable("volunteer");

        // Primary key
        builder.HasKey(x => x.VolunteerId);
        builder.Property(x => x.VolunteerId)
            .HasColumnName("volunteer_id")
            .IsRequired()
            .ValueGeneratedOnAdd();

        // Foreign keys
        builder.Property(x => x.ResearchNodeId)
            .HasColumnName("research_node_id")
            .IsRequired();

        // Basic properties
        builder.Property(x => x.VolunteerCode)
            .HasColumnName("volunteer_code")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.BirthDate)
            .HasColumnName("birth_date")
            .IsRequired();

        builder.Property(x => x.Gender)
            .HasColumnName("gender")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.BloodType)
            .HasColumnName("blood_type")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.Height)
            .HasColumnName("height");

        builder.Property(x => x.Weight)
            .HasColumnName("weight");

        builder.Property(x => x.MedicalHistory)
            .HasColumnName("medical_history")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.ConsentStatus)
            .HasColumnName("consent_status")
            .HasMaxLength(50)
            .IsRequired();

        // Timestamps
        builder.Property(x => x.EnrolledAt)
            .HasColumnName("enrolled_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Relationships
        builder.HasOne(x => x.ResearchNode)
            .WithMany()
            .HasForeignKey(x => x.ResearchNodeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.ResearchNodeId)
            .HasDatabaseName("ix_volunteer_research_node_id");

        builder.HasIndex(x => x.VolunteerCode)
            .IsUnique()
            .HasDatabaseName("ix_volunteer_code");

        builder.HasIndex(x => x.ConsentStatus)
            .HasDatabaseName("ix_volunteer_consent_status");
    }
}
