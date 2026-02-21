using Bioteca.Prism.Domain.Entities.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bioteca.Prism.Data.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for SyncLog entity
/// </summary>
public class SyncLogConfiguration : IEntityTypeConfiguration<SyncLog>
{
    public void Configure(EntityTypeBuilder<SyncLog> builder)
    {
        builder.ToTable("sync_logs");

        // Primary key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedOnAdd();

        // Foreign key
        builder.Property(x => x.RemoteNodeId)
            .HasColumnName("remote_node_id")
            .IsRequired();

        // Timestamps
        builder.Property(x => x.StartedAt)
            .HasColumnName("started_at")
            .IsRequired();

        builder.Property(x => x.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(x => x.LastSyncedAt)
            .HasColumnName("last_synced_at");

        // Status
        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(50)
            .IsRequired();

        // JSON payload (stored as jsonb)
        builder.Property(x => x.EntitiesReceived)
            .HasColumnName("entities_received")
            .HasColumnType("jsonb");

        // Error details
        builder.Property(x => x.ErrorMessage)
            .HasColumnName("error_message")
            .HasColumnType("text");

        // Relationships
        builder.HasOne(x => x.RemoteNode)
            .WithMany()
            .HasForeignKey(x => x.RemoteNodeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(x => x.RemoteNodeId)
            .HasDatabaseName("ix_sync_logs_remote_node_id");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("ix_sync_logs_status");

        builder.HasIndex(x => x.StartedAt)
            .HasDatabaseName("ix_sync_logs_started_at");
    }
}
