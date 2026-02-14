
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
        // Prevent circular reference errors when serializing entities with navigation properties
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Enter your token in the text input below."
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

DependencyInjectionConfig.AddDependencyInjectionConfiguration(builder.Services);

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowViteDevelopment", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()
              .WithExposedHeaders("X-Channel-Id");
    });

    // Allow Electron desktop app (no Origin header or file:// origin)
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("X-Channel-Id");
    });
});

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
{
    options.UseNpgsql(connectionString);
});



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

// Use CORS policy - AllowAll for development (includes Electron and Vite)
// In production, this should be restricted to specific origins
if (app.Environment.IsDevelopment() ||
    app.Environment.EnvironmentName == "NodeA" ||
    app.Environment.EnvironmentName == "NodeB")
{
    app.UseCors("AllowAll");
}
else
{
    app.UseCors("AllowViteDevelopment");
}

app.UseAuthorization();

app.MapControllers();

app.Run();

// Make the implicit Program class public for testing
public partial class Program { }