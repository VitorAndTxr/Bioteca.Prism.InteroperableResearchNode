using Bioteca.Prism.Domain.Entities.Record;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bioteca.Prism.Data.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for SessionAnnotation entity
/// </summary>
public class SessionAnnotationConfiguration : IEntityTypeConfiguration<SessionAnnotation>
{
    public void Configure(EntityTypeBuilder<SessionAnnotation> builder)
    {
        builder.ToTable("session_annotation");

        // Primary key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedOnAdd();

        // Foreign keys
        builder.Property(x => x.RecordSessionId)
            .HasColumnName("record_session_id")
            .IsRequired();

        // Basic properties
        builder.Property(x => x.Text)
            .HasColumnName("text")
            .HasMaxLength(5000)
            .IsRequired();

        // Timestamps
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Relationships
        builder.HasOne(x => x.RecordSession)
            .WithMany(x => x.SessionAnnotations)
            .HasForeignKey(x => x.RecordSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.RecordSessionId)
            .HasDatabaseName("ix_session_annotation_record_session_id");

        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("ix_session_annotation_created_at");
    }
}
