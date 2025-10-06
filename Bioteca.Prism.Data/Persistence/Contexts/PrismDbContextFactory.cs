using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Bioteca.Prism.Data.Persistence.Contexts;

/// <summary>
/// Design-time factory for creating PrismDbContext instances for EF Core migrations
/// </summary>
/// <remarks>
/// To create migrations for a specific node, use:
/// - Node A: dotnet ef migrations add MigrationName -- --node=a
/// - Node B: dotnet ef migrations add MigrationName -- --node=b
/// Default: Node A
/// </remarks>
public class PrismDbContextFactory : IDesignTimeDbContextFactory<PrismDbContext>
{
    public PrismDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PrismDbContext>();

        // Determine which node's database to use based on arguments
        // Default to Node A if not specified
        var node = "a";
        if (args.Length > 0)
        {
            var nodeArg = args.FirstOrDefault(a => a.StartsWith("--node="));
            if (nodeArg != null)
            {
                node = nodeArg.Split('=')[1].ToLower();
            }
        }

        // Use environment variable if provided (useful for CI/CD)
        var envNode = Environment.GetEnvironmentVariable("PRISM_DB_NODE");
        if (!string.IsNullOrEmpty(envNode))
        {
            node = envNode.ToLower();
        }

        // Build connection string based on node
        string connectionString;
        if (node == "b")
        {
            connectionString = "Host=localhost;Port=5433;Database=prism_node_b_registry;Username=prism_user_b;Password=prism_secure_password_2025_b";
        }
        else // Default to Node A
        {
            connectionString = "Host=localhost;Port=5432;Database=prism_node_a_registry;Username=prism_user_a;Password=prism_secure_password_2025_a";
        }

        optionsBuilder.UseNpgsql(connectionString);

        return new PrismDbContext(optionsBuilder.Options);
    }
}
