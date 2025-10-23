using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Bioteca.Prism.Data.Persistence.Contexts;

/// <summary>
/// Design-time factory for PrismDbContext to support EF Core migrations
/// Supports multiple node configurations (NodeA, NodeB) via environment variable or command-line argument
/// </summary>
/// <remarks>
/// Usage:
/// - Set environment variable: ASPNETCORE_ENVIRONMENT=NodeA
/// - Or pass argument: dotnet ef migrations add MigrationName -- --node NodeA
/// - Or pass argument: dotnet ef migrations add MigrationName -- --node NodeB
/// - Default: NodeA
/// </remarks>
public class PrismDbContextFactory : IDesignTimeDbContextFactory<PrismDbContext>
{
    public PrismDbContext CreateDbContext(string[] args)
    {
        // Determine which node configuration to use
        // Priority: 1) Command-line args, 2) Environment variable, 3) Default (NodeA)
        string nodeProfile = DetermineNodeProfile(args);

        Console.WriteLine($"[PrismDbContextFactory] Using configuration profile: {nodeProfile}");

        // Get the startup project directory (where appsettings files are located)
        var startupProjectPath = Path.GetFullPath(
            Path.Combine(Directory.GetCurrentDirectory(), "..", "Bioteca.Prism.InteroperableResearchNode"));

        Console.WriteLine($"[PrismDbContextFactory] Looking for configuration in: {startupProjectPath}");

        // Build full paths to configuration files
        var baseConfigPath = Path.Combine(startupProjectPath, "appsettings.json");
        var nodeConfigPath = Path.Combine(startupProjectPath, $"appsettings.{nodeProfile}.json");

        // Build configuration with node-specific settings
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(baseConfigPath, optional: true)
            .AddJsonFile(nodeConfigPath, optional: false)
            .AddEnvironmentVariables()
            .Build();

        // Get connection string from node-specific configuration
        var connectionString = configuration.GetConnectionString("PrismDatabase");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string 'PrismDatabase' not found in appsettings.{nodeProfile}.json. " +
                $"Please ensure the configuration file exists in {startupProjectPath}");
        }

        Console.WriteLine($"[PrismDbContextFactory] Using connection string: {MaskConnectionString(connectionString)}");

        // Build DbContext options
        var optionsBuilder = new DbContextOptionsBuilder<PrismDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public");
        });

        return new PrismDbContext(optionsBuilder.Options);
    }

    /// <summary>
    /// Determines which node profile to use based on arguments or environment
    /// </summary>
    private static string DetermineNodeProfile(string[] args)
    {
        // Check command-line arguments for --node parameter
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i].Equals("--node", StringComparison.OrdinalIgnoreCase))
            {
                var nodeArg = args[i + 1];
                if (nodeArg.Equals("NodeA", StringComparison.OrdinalIgnoreCase) ||
                    nodeArg.Equals("A", StringComparison.OrdinalIgnoreCase))
                {
                    return "NodeA";
                }
                if (nodeArg.Equals("NodeB", StringComparison.OrdinalIgnoreCase) ||
                    nodeArg.Equals("B", StringComparison.OrdinalIgnoreCase))
                {
                    return "NodeB";
                }
            }
        }

        // Check environment variable
        var envProfile = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (!string.IsNullOrEmpty(envProfile))
        {
            if (envProfile.Equals("NodeA", StringComparison.OrdinalIgnoreCase))
                return "NodeA";
            if (envProfile.Equals("NodeB", StringComparison.OrdinalIgnoreCase))
                return "NodeB";
        }

        // Default to NodeA
        Console.WriteLine("[PrismDbContextFactory] No node specified, defaulting to NodeA");
        Console.WriteLine("[PrismDbContextFactory] Tip: Use --node NodeA or --node NodeB to specify target node");
        return "NodeA";
    }

    /// <summary>
    /// Masks sensitive information in connection string for logging
    /// </summary>
    private static string MaskConnectionString(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return connectionString;

        // Mask password
        var parts = connectionString.Split(';');
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].TrimStart().StartsWith("Password=", StringComparison.OrdinalIgnoreCase))
            {
                parts[i] = "Password=***";
            }
        }
        return string.Join(";", parts);
    }
}
