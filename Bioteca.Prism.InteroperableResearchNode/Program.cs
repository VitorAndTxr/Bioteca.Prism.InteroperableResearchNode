using Bioteca.Prism.Core.Cache;
using Bioteca.Prism.Core.Cache.Session;
using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Middleware.Channel;
using Bioteca.Prism.Core.Middleware.Node;
using Bioteca.Prism.Core.Middleware.Session;
using Bioteca.Prism.Core.Security.Cryptography;
using Bioteca.Prism.Data.Interfaces.Application;
using Bioteca.Prism.Data.Interfaces.Device;
using Bioteca.Prism.Data.Interfaces.Node;
using Bioteca.Prism.Data.Interfaces.Record;
using Bioteca.Prism.Data.Interfaces.Research;
using Bioteca.Prism.Data.Interfaces.Researcher;
using Bioteca.Prism.Data.Interfaces.Sensor;
using Bioteca.Prism.Data.Interfaces.Snomed;
using Bioteca.Prism.Data.Interfaces.Volunteer;
using Bioteca.Prism.Data.Persistence.Contexts;
using Bioteca.Prism.Data.Repositories.Application;
using Bioteca.Prism.Data.Repositories.Device;
using Bioteca.Prism.Data.Repositories.Node;
using Bioteca.Prism.Data.Repositories.Record;
using Bioteca.Prism.Data.Repositories.Research;
using Bioteca.Prism.Data.Repositories.Researcher;
using Bioteca.Prism.Data.Repositories.Sensor;
using Bioteca.Prism.Data.Repositories.Snomed;
using Bioteca.Prism.Data.Repositories.Volunteer;
using Bioteca.Prism.Service.Interfaces.Application;
using Bioteca.Prism.Service.Interfaces.Device;
using Bioteca.Prism.Service.Interfaces.Record;
using Bioteca.Prism.Service.Interfaces.Research;
using Bioteca.Prism.Service.Interfaces.Researcher;
using Bioteca.Prism.Service.Interfaces.Sensor;
using Bioteca.Prism.Service.Interfaces.Snomed;
using Bioteca.Prism.Service.Interfaces.Volunteer;
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

// Configure HttpClient with timeout from configuration (default: 5 minutes)
var httpTimeoutSeconds = builder.Configuration.GetValue<int>("HttpClient:TimeoutSeconds", 300);
builder.Services.AddHttpClient(string.Empty, client =>
{
    client.Timeout = TimeSpan.FromSeconds(httpTimeoutSeconds);
});

builder.Services.AddSingleton<INodeChannelClient, NodeChannelClient>();


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

// Register repositories
builder.Services.AddScoped<INodeRepository, NodeRepository>();

// Research data repositories
builder.Services.AddScoped<IResearchRepository, ResearchRepository>();
builder.Services.AddScoped<IVolunteerRepository, VolunteerRepository>();
builder.Services.AddScoped<IResearcherRepository, ResearcherRepository>();
builder.Services.AddScoped<IApplicationRepository, ApplicationRepository>();
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
builder.Services.AddScoped<ISensorRepository, SensorRepository>();

// Record repositories
builder.Services.AddScoped<IRecordSessionRepository, RecordSessionRepository>();
builder.Services.AddScoped<IRecordRepository, RecordRepository>();
builder.Services.AddScoped<IRecordChannelRepository, RecordChannelRepository>();
builder.Services.AddScoped<ITargetAreaRepository, TargetAreaRepository>();

// SNOMED CT repositories
builder.Services.AddScoped<ISnomedLateralityRepository, SnomedLateralityRepository>();
builder.Services.AddScoped<ISnomedTopographicalModifierRepository, SnomedTopographicalModifierRepository>();
builder.Services.AddScoped<ISnomedBodyRegionRepository, SnomedBodyRegionRepository>();
builder.Services.AddScoped<ISnomedBodyStructureRepository, SnomedBodyStructureRepository>();

// Register services (business logic layer)
// Research data services
builder.Services.AddScoped<IResearchService, Bioteca.Prism.Service.Services.Research.ResearchService>();
builder.Services.AddScoped<IVolunteerService, Bioteca.Prism.Service.Services.Volunteer.VolunteerService>();
builder.Services.AddScoped<IResearcherService, Bioteca.Prism.Service.Services.Researcher.ResearcherService>();
builder.Services.AddScoped<IApplicationService, Bioteca.Prism.Service.Services.Application.ApplicationService>();
builder.Services.AddScoped<IDeviceService, Bioteca.Prism.Service.Services.Device.DeviceService>();
builder.Services.AddScoped<ISensorService, Bioteca.Prism.Service.Services.Sensor.SensorService>();

// Record services
builder.Services.AddScoped<IRecordSessionService, Bioteca.Prism.Service.Services.Record.RecordSessionService>();
builder.Services.AddScoped<IRecordService, Bioteca.Prism.Service.Services.Record.RecordService>();
builder.Services.AddScoped<IRecordChannelService, Bioteca.Prism.Service.Services.Record.RecordChannelService>();
builder.Services.AddScoped<ITargetAreaService, Bioteca.Prism.Service.Services.Record.TargetAreaService>();

// SNOMED CT services
builder.Services.AddScoped<ISnomedLateralityService, Bioteca.Prism.Service.Services.Snomed.SnomedLateralityService>();
builder.Services.AddScoped<ISnomedTopographicalModifierService, Bioteca.Prism.Service.Services.Snomed.SnomedTopographicalModifierService>();
builder.Services.AddScoped<ISnomedBodyRegionService, Bioteca.Prism.Service.Services.Snomed.SnomedBodyRegionService>();
builder.Services.AddScoped<ISnomedBodyStructureService, Bioteca.Prism.Service.Services.Snomed.SnomedBodyStructureService>();

// Register PostgreSQL-backed node registry service
builder.Services.AddScoped<IResearchNodeService, ResearchNodeService>();



// Register Phase 3 services (Mutual Authentication)
builder.Services.AddSingleton<IChallengeService, ChallengeService>();

// Register Redis infrastructure (conditionally)
var useRedisForSessions = builder.Configuration.GetValue<bool>("FeatureFlags:UseRedisForSessions");
var useRedisForChannels = builder.Configuration.GetValue<bool>("FeatureFlags:UseRedisForChannels");


builder.Services.AddSingleton<IRedisConnectionService, RedisConnectionService>();



builder.Services.AddSingleton<IChannelStore, RedisChannelStore>();
builder.Services.AddSingleton<ISessionStore, RedisSessionStore>();
builder.Services.AddSingleton<ISessionService, SessionService>();

var app = builder.Build();

// Apply database migrations on startup (if using PostgreSQL)

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