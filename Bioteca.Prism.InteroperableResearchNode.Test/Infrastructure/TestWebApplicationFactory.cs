using Bioteca.Prism.Core.Middleware.Node;
using Bioteca.Prism.Data.Persistence.Contexts;
using Bioteca.Prism.Data.Repositories.Node;
using Bioteca.Prism.Service.Services.Node;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bioteca.Prism.InteroperableResearchNode.Test.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory for testing with configurable storage backend
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly bool _useInMemoryDatabase;
    private readonly string _databaseName;

    public TestWebApplicationFactory(bool useInMemoryDatabase = true, string? databaseName = null)
    {
        _useInMemoryDatabase = useInMemoryDatabase;
        _databaseName = databaseName ?? Guid.NewGuid().ToString();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Use Test environment
        builder.UseEnvironment("Test");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Clear existing configuration
            config.Sources.Clear();

            // Add base configuration
            config.AddJsonFile("appsettings.json", optional: true);
            config.AddJsonFile("appsettings.Test.json", optional: true);

            // Override configuration to disable PostgreSQL and Redis for tests
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureFlags:UsePostgreSqlForNodes"] = "false",
                ["FeatureFlags:UseRedisForSessions"] = "false",
                ["FeatureFlags:UseRedisForChannels"] = "false",
                ["Redis:EnableRedis"] = "false"
            });
        });

        builder.ConfigureServices(services =>
        {
            if (_useInMemoryDatabase)
            {
                // Remove PostgreSQL DbContext registration if it exists
                var dbContextDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<PrismDbContext>));

                if (dbContextDescriptor != null)
                {
                    services.Remove(dbContextDescriptor);
                }

                var dbContextServiceDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(PrismDbContext));

                if (dbContextServiceDescriptor != null)
                {
                    services.Remove(dbContextServiceDescriptor);
                }

                // Remove repository if registered
                var repositoryDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(INodeRepository));

                if (repositoryDescriptor != null)
                {
                    services.Remove(repositoryDescriptor);
                }

                // Remove PostgreSQL node registry service if registered
                var nodeRegistryDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(INodeRegistryService) &&
                         d.ImplementationType == typeof(PostgreSqlNodeRegistryService));

                if (nodeRegistryDescriptor != null)
                {
                    services.Remove(nodeRegistryDescriptor);
                }

                // Register in-memory node registry service for tests (already registered by default)
                // No need to re-register since it's the fallback
            }
            else
            {
                // For PostgreSQL tests, replace with in-memory EF provider
                // Remove the PostgreSQL DbContext
                var dbContextDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<PrismDbContext>));

                if (dbContextDescriptor != null)
                {
                    services.Remove(dbContextDescriptor);
                }

                var dbContextServiceDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(PrismDbContext));

                if (dbContextServiceDescriptor != null)
                {
                    services.Remove(dbContextServiceDescriptor);
                }

                // Add in-memory EF database provider for testing
                services.AddDbContext<PrismDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_databaseName);
                });

                // Ensure repository is registered
                var repositoryDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(INodeRepository));

                if (repositoryDescriptor == null)
                {
                    services.AddScoped<INodeRepository, NodeRepository>();
                }

                // Ensure PostgreSQL node registry service is registered
                var nodeRegistryDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(INodeRegistryService));

                if (nodeRegistryDescriptor != null)
                {
                    services.Remove(nodeRegistryDescriptor);
                }

                services.AddScoped<INodeRegistryService, PostgreSqlNodeRegistryService>();
            }
        });
    }

    /// <summary>
    /// Get a scoped service from the test server
    /// </summary>
    public T GetScopedService<T>() where T : notnull
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Seed the database with test data
    /// </summary>
    public void SeedDatabase(Action<IServiceProvider> seedAction)
    {
        using var scope = Services.CreateScope();
        seedAction(scope.ServiceProvider);
    }
}
