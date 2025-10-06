using Bioteca.Prism.Core.Cache;
using Bioteca.Prism.Core.Cache.Session;
using Bioteca.Prism.Core.Middleware.Channel;
using Bioteca.Prism.Core.Middleware.Node;
using Bioteca.Prism.Core.Middleware.Session;
using Bioteca.Prism.Core.Security.Cryptography;
using Bioteca.Prism.Core.Security.Cryptography.Interfaces;
using Bioteca.Prism.Data.Cache.Channel;
using Bioteca.Prism.Data.Persistence.Contexts;
using Bioteca.Prism.Data.Repositories.Node;
using Bioteca.Prism.Service.Services.Cache;
using Bioteca.Prism.Service.Services.Node;
using Bioteca.Prism.Service.Services.Session;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Use camelCase for JSON properties (JavaScript convention)
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        // Case-insensitive property matching for deserialization
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        // Allow trailing commas in JSON
        options.JsonSerializerOptions.AllowTrailingCommas = true;
    });
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register Phase 1 services (Channel Establishment)
builder.Services.AddSingleton<IEphemeralKeyService, EphemeralKeyService>();
builder.Services.AddSingleton<IChannelEncryptionService, ChannelEncryptionService>();
builder.Services.AddHttpClient(); // Required for NodeChannelClient
builder.Services.AddSingleton<INodeChannelClient, NodeChannelClient>();

// Register PostgreSQL infrastructure (conditionally)
var usePostgreSqlForNodes = builder.Configuration.GetValue<bool>("FeatureFlags:UsePostgreSqlForNodes");

if (usePostgreSqlForNodes)
{
    // Register DbContext with PostgreSQL
    var connectionString = builder.Configuration.GetConnectionString("PrismDatabase");
    builder.Services.AddDbContext<PrismDbContext>(options =>
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);
        }));

    // Register repository
    builder.Services.AddScoped<INodeRepository, NodeRepository>();

    // Register PostgreSQL-backed node registry service
    builder.Services.AddScoped<Bioteca.Prism.Core.Middleware.Node.INodeRegistryService, PostgreSqlNodeRegistryService>();
}
else
{
    // Register in-memory node registry service (default)
    builder.Services.AddSingleton<Bioteca.Prism.Core.Middleware.Node.INodeRegistryService, NodeRegistryService>();
}

// Register Phase 3 services (Mutual Authentication)
builder.Services.AddSingleton<IChallengeService, ChallengeService>();

// Register Redis infrastructure (conditionally)
var useRedisForSessions = builder.Configuration.GetValue<bool>("FeatureFlags:UseRedisForSessions");
var useRedisForChannels = builder.Configuration.GetValue<bool>("FeatureFlags:UseRedisForChannels");

if (useRedisForSessions || useRedisForChannels)
{
    // Redis connection service (shared between sessions and channels)
    builder.Services.AddSingleton<IRedisConnectionService, RedisConnectionService>();
}

// Register Channel Store (Phase 1)
if (useRedisForChannels)
{
    builder.Services.AddSingleton<IChannelStore, RedisChannelStore>();
}
else
{
    builder.Services.AddSingleton<IChannelStore, ChannelStore>(); // In-memory (default)
}

// Register Session Store (Phase 4)
if (useRedisForSessions)
{
    builder.Services.AddSingleton<ISessionStore, RedisSessionStore>();
}
else
{
    builder.Services.AddSingleton<ISessionStore, InMemorySessionStore>(); // In-memory (default)
}

// Register Phase 4 services (Session Management)
builder.Services.AddSingleton<ISessionService, SessionService>();

var app = builder.Build();

// Apply database migrations on startup (if using PostgreSQL)
if (usePostgreSqlForNodes)
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<PrismDbContext>();
        try
        {
            dbContext.Database.Migrate();
            app.Logger.LogInformation("Database migrations applied successfully");
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "Error applying database migrations");
        }
    }
}

// Configure the HTTP request pipeline.
// Enable Swagger in Development, NodeA, and NodeB environments
if (app.Environment.IsDevelopment() ||
    app.Environment.EnvironmentName == "NodeA" ||
    app.Environment.EnvironmentName == "NodeB")
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Only use HTTPS redirection in Production
if (app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

app.Run();

// Make the implicit Program class public for testing
public partial class Program { }