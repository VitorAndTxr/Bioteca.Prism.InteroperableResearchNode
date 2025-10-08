using Bioteca.Prism.Domain.Entities.Device;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bioteca.Prism.Data.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for Device entity
/// </summary>
public class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.ToTable("device");

        // Primary key
        builder.HasKey(x => x.DeviceId);
        builder.Property(x => x.DeviceId)
            .HasColumnName("device_id")
            .IsRequired()
            .ValueGeneratedOnAdd();

        // Foreign keys
        builder.Property(x => x.ResearchId)
            .HasColumnName("research_id")
            .IsRequired();

        // Basic properties
        builder.Property(x => x.DeviceName)
            .HasColumnName("device_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Manufacturer)
            .HasColumnName("manufacturer")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Model)
            .HasColumnName("model")
            .HasMaxLength(200)
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
        builder.HasOne(x => x.Research)
            .WithMany(x => x.Devices)
            .HasForeignKey(x => x.ResearchId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.ResearchId)
            .HasDatabaseName("ix_device_research_id");
    }
}
