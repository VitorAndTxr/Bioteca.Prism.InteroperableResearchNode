using Bioteca.Prism.Domain.Entities.Volunteer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bioteca.Prism.Data.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for VitalSigns
/// </summary>
public class VitalSignsConfiguration : IEntityTypeConfiguration<VitalSigns>
{
    public void Configure(EntityTypeBuilder<VitalSigns> builder)
    {
        builder.ToTable("vital_signs");

        // Primary key
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        // Foreign keys
        builder.Property(x => x.VolunteerId)
            .HasColumnName("volunteer_id")
            .IsRequired();

        builder.Property(x => x.RecordSessionId)
            .HasColumnName("record_session_id")
            .IsRequired();

        builder.Property(x => x.RecordedBy)
            .HasColumnName("recorded_by")
            .IsRequired();

        // Properties
        builder.Property(x => x.SystolicBp)
            .HasColumnName("systolic_bp")
            .IsRequired(false);

        builder.Property(x => x.DiastolicBp)
            .HasColumnName("diastolic_bp")
            .IsRequired(false);

        builder.Property(x => x.HeartRate)
            .HasColumnName("heart_rate")
            .IsRequired(false);

        builder.Property(x => x.RespiratoryRate)
            .HasColumnName("respiratory_rate")
            .IsRequired(false);

        builder.Property(x => x.Temperature)
            .HasColumnName("temperature")
            .IsRequired(false);

        builder.Property(x => x.OxygenSaturation)
            .HasColumnName("oxygen_saturation")
            .IsRequired(false);

        builder.Property(x => x.Weight)
            .HasColumnName("weight")
            .IsRequired(false);

        builder.Property(x => x.Height)
            .HasColumnName("height")
            .IsRequired(false);

        builder.Property(x => x.Bmi)
            .HasColumnName("bmi")
            .IsRequired(false);

        builder.Property(x => x.MeasurementDatetime)
            .HasColumnName("measurement_datetime")
            .IsRequired();

        builder.Property(x => x.MeasurementContext)
            .HasColumnName("measurement_context")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Relationships
        builder.HasOne(x => x.Volunteer)
            .WithMany(x => x.VitalSigns)
            .HasForeignKey(x => x.VolunteerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.RecordSession)
            .WithMany(x => x.VitalSigns)
            .HasForeignKey(x => x.RecordSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.VolunteerId)
            .HasDatabaseName("ix_vital_signs_volunteer_id");

        builder.HasIndex(x => x.RecordSessionId)
            .HasDatabaseName("ix_vital_signs_record_session_id");

        builder.HasIndex(x => x.MeasurementDatetime)
            .HasDatabaseName("ix_vital_signs_measurement_datetime");

        builder.HasIndex(x => x.RecordedBy)
            .HasDatabaseName("ix_vital_signs_recorded_by");
    }
}
