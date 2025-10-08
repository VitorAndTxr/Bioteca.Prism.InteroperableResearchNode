using Bioteca.Prism.Domain.Entities.Device;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bioteca.Prism.Data.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for ResearchDevice join entity
/// </summary>
public class ResearchDeviceConfiguration : IEntityTypeConfiguration<ResearchDevice>
{
    public void Configure(EntityTypeBuilder<ResearchDevice> builder)
    {
        builder.ToTable("research_device");

        // Composite primary key
        builder.HasKey(x => new { x.ResearchId, x.DeviceId });

        // Foreign keys
        builder.Property(x => x.ResearchId)
            .HasColumnName("research_id")
            .IsRequired();

        builder.Property(x => x.DeviceId)
            .HasColumnName("device_id")
            .IsRequired();

        // Properties
        builder.Property(x => x.Role)
            .HasColumnName("role")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.AddedAt)
            .HasColumnName("added_at")
            .IsRequired();

        builder.Property(x => x.RemovedAt)
            .HasColumnName("removed_at")
            .IsRequired(false);

        builder.Property(x => x.CalibrationStatus)
            .HasColumnName("calibration_status")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.LastCalibrationDate)
            .HasColumnName("last_calibration_date")
            .IsRequired(false);

        // Relationships
        builder.HasOne(x => x.Research)
            .WithMany(x => x.ResearchDevices)
            .HasForeignKey(x => x.ResearchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Device)
            .WithMany(x => x.ResearchDevices)
            .HasForeignKey(x => x.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.ResearchId)
            .HasDatabaseName("ix_research_device_research_id");

        builder.HasIndex(x => x.DeviceId)
            .HasDatabaseName("ix_research_device_device_id");
    }
}
