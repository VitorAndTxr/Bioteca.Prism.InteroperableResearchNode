using Bioteca.Prism.Core.Middleware.Channel;
using Bioteca.Prism.Core.Security.Cryptography;
using Bioteca.Prism.Core.Security.Cryptography.Interfaces;
using Bioteca.Prism.Data.Cache.Channel;
using Bioteca.Prism.Service.Interfaces.Node;
using Bioteca.Prism.Service.Services.Node;

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
builder.Services.AddSingleton<IChannelStore, ChannelStore>(); // Centralized channel management
builder.Services.AddHttpClient(); // Required for NodeChannelClient
builder.Services.AddSingleton<INodeChannelClient, NodeChannelClient>();

// Register Phase 2 services (Node Identification and Authorization)
builder.Services.AddSingleton<Bioteca.Prism.Core.Middleware.Node.INodeRegistryService, NodeRegistryService>();

var app = builder.Build();

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