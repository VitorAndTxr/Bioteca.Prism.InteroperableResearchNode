using Bioteca.Prism.Domain.Entities.Record;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bioteca.Prism.Data.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for RecordChannel entity
/// </summary>
public class RecordChannelConfiguration : IEntityTypeConfiguration<RecordChannel>
{
    public void Configure(EntityTypeBuilder<RecordChannel> builder)
    {
        builder.ToTable("record_channel");

        // Primary key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedOnAdd();

        // Foreign keys
        builder.Property(x => x.RecordId)
            .HasColumnName("record_id")
            .IsRequired();

        builder.Property(x => x.SensorId)
            .HasColumnName("sensor_id");

        // Basic properties
        builder.Property(x => x.SignalType)
            .HasColumnName("signal_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.FileUrl)
            .HasColumnName("file_url")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.SamplingRate)
            .HasColumnName("sampling_rate")
            .IsRequired();

        builder.Property(x => x.SamplesCount)
            .HasColumnName("samples_count")
            .IsRequired();

        builder.Property(x => x.StartTimestamp)
            .HasColumnName("start_timestamp")
            .IsRequired();

        builder.Property(x => x.Annotations)
            .HasColumnName("annotations")
            .HasColumnType("jsonb");

        // Timestamps
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Relationships
        builder.HasOne(x => x.Record)
            .WithMany(x => x.RecordChannels)
            .HasForeignKey(x => x.RecordId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Sensor)
            .WithMany(x => x.RecordChannels)
            .HasForeignKey(x => x.SensorId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(x => x.RecordId)
            .HasDatabaseName("ix_record_channel_record_id");

        builder.HasIndex(x => x.SensorId)
            .HasDatabaseName("ix_record_channel_sensor_id");

        builder.HasIndex(x => x.SignalType)
            .HasDatabaseName("ix_record_channel_signal_type");
    }
}
