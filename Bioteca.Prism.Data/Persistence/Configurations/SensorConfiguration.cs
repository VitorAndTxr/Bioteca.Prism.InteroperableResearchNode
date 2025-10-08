using Bioteca.Prism.Domain.Entities.Sensor;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bioteca.Prism.Data.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for Sensor entity
/// </summary>
public class SensorConfiguration : IEntityTypeConfiguration<Sensor>
{
    public void Configure(EntityTypeBuilder<Sensor> builder)
    {
        builder.ToTable("sensor");

        // Primary key
        builder.HasKey(x => x.SensorId);
        builder.Property(x => x.SensorId)
            .HasColumnName("sensor_id")
            .IsRequired()
            .ValueGeneratedOnAdd();

        // Foreign keys
        builder.Property(x => x.DeviceId)
            .HasColumnName("device_id")
            .IsRequired();

        // Basic properties
        builder.Property(x => x.SensorName)
            .HasColumnName("sensor_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.MaxSamplingRate)
            .HasColumnName("max_sampling_rate")
            .IsRequired();

        builder.Property(x => x.Unit)
            .HasColumnName("unit")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.MinRange)
            .HasColumnName("min_range")
            .IsRequired();

        builder.Property(x => x.MaxRange)
            .HasColumnName("max_range")
            .IsRequired();

        builder.Property(x => x.Accuracy)
            .HasColumnName("accuracy")
            .IsRequired();

        builder.Property(x => x.AdditionalInfo)
            .HasColumnName("additional_info")
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
        builder.HasOne(x => x.Device)
            .WithMany(x => x.Sensors)
            .HasForeignKey(x => x.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.DeviceId)
            .HasDatabaseName("ix_sensor_device_id");
    }
}
