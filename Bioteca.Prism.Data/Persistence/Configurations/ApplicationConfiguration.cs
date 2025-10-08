using Bioteca.Prism.Domain.Entities.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bioteca.Prism.Data.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for Application entity
/// </summary>
public class ApplicationConfiguration : IEntityTypeConfiguration<Application>
{
    public void Configure(EntityTypeBuilder<Application> builder)
    {
        builder.ToTable("application");

        // Primary key
        builder.HasKey(x => x.ApplicationId);
        builder.Property(x => x.ApplicationId)
            .HasColumnName("application_id")
            .IsRequired()
            .ValueGeneratedOnAdd();

        // Basic properties
        builder.Property(x => x.AppName)
            .HasColumnName("app_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Url)
            .HasColumnName("url")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("text")
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
    }
}
