
using Bioteca.Prism.Core.Middleware.Node;

using Bioteca.Prism.Data.Persistence.Contexts;
using Bioteca.Prism.InteroperableResearchNode.Config;
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

DependencyInjectionConfig.AddDependencyInjectionConfiguration(builder.Services);

// Configure HttpClient with timeout from configuration (default: 5 minutes)
var httpTimeoutSeconds = builder.Configuration.GetValue<int>("HttpClient:TimeoutSeconds", 300);
builder.Services.AddHttpClient(string.Empty, client =>
{
    client.Timeout = TimeSpan.FromSeconds(httpTimeoutSeconds);
});

builder.Services.AddSingleton<INodeChannelClient, NodeChannelClient>();


// Register DbContext with PostgreSQL



// Register Redis infrastructure (conditionally)
var useRedisForSessions = builder.Configuration.GetValue<bool>("FeatureFlags:UseRedisForSessions");
var useRedisForChannels = builder.Configuration.GetValue<bool>("FeatureFlags:UseRedisForChannels");

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