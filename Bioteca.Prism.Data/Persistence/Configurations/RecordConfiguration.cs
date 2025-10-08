using Bioteca.Prism.Domain.Entities.Record;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bioteca.Prism.Data.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for Record entity
/// </summary>
public class RecordConfiguration : IEntityTypeConfiguration<Record>
{
    public void Configure(EntityTypeBuilder<Record> builder)
    {
        builder.ToTable("record");

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
        builder.Property(x => x.CollectionDate)
            .HasColumnName("collection_date")
            .IsRequired();

        builder.Property(x => x.SessionId)
            .HasColumnName("session_id")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.RecordType)
            .HasColumnName("record_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasColumnName("notes")
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
        builder.HasOne(x => x.RecordSession)
            .WithMany(x => x.Records)
            .HasForeignKey(x => x.RecordSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.RecordSessionId)
            .HasDatabaseName("ix_record_record_session_id");

        builder.HasIndex(x => x.CollectionDate)
            .HasDatabaseName("ix_record_collection_date");

        builder.HasIndex(x => x.RecordType)
            .HasDatabaseName("ix_record_record_type");
    }
}
