using Bioteca.Prism.Domain.Entities.Node;
using Bioteca.Prism.Domain.Enumerators.Node;
using Bioteca.Prism.Domain.Responses.Node;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Bioteca.Prism.Data.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for ResearchNode entity
/// </summary>
public class ResearchNodeConfiguration : IEntityTypeConfiguration<ResearchNode>
{
    public void Configure(EntityTypeBuilder<ResearchNode> builder)
    {
        builder.ToTable("research_nodes");

        // Primary key (Guid)
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedOnAdd();

        // Basic properties
        builder.Property(x => x.NodeName)
            .HasColumnName("node_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Certificate)
            .HasColumnName("certificate")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.CertificateFingerprint)
            .HasColumnName("certificate_fingerprint")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.NodeUrl)
            .HasColumnName("node_url")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.ContactInfo)
            .HasColumnName("contact_info")
            .HasMaxLength(500);

        builder.Property(x => x.InstitutionDetails)
            .HasColumnName("institution_details")
            .HasMaxLength(1000);

        // Enums
        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.NodeAccessLevel)
            .HasColumnName("node_access_level")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // Timestamps
        builder.Property(x => x.RegisteredAt)
            .HasColumnName("registered_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(x => x.LastAuthenticatedAt)
            .HasColumnName("last_authenticated_at");

        // Metadata as JSONB (PostgreSQL native JSON type)
        builder.Property(x => x.Metadata)
            .HasColumnName("metadata")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, string>()
            );

        // Indexes
        builder.HasIndex(x => x.CertificateFingerprint)
            .IsUnique()
            .HasDatabaseName("ix_research_nodes_certificate_fingerprint");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("ix_research_nodes_status");

        builder.HasIndex(x => x.NodeAccessLevel)
            .HasDatabaseName("ix_research_nodes_access_level");

        builder.HasIndex(x => x.RegisteredAt)
            .HasDatabaseName("ix_research_nodes_registered_at");

        builder.HasIndex(x => x.LastAuthenticatedAt)
            .HasDatabaseName("ix_research_nodes_last_authenticated_at");
    }
}
