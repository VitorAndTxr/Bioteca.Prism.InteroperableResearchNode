using Bioteca.Prism.Data.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Bioteca.Prism.InteroperableResearchNode.Config
{
    public static class DatabaseConfig
    {
        public static void AddDatabaseConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            var connectionString = configuration.GetConnectionString("PrismDatabase");
            services.AddDbContext<PrismDbContext>(options =>
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null);
                }));
        }
    }
}
