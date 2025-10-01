using Bioteca.Prism.Service.Interfaces.Node;
using Bioteca.Prism.Service.Services.Node;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register Phase 1 services (Channel Establishment)
builder.Services.AddSingleton<IEphemeralKeyService, EphemeralKeyService>();
builder.Services.AddSingleton<IChannelEncryptionService, ChannelEncryptionService>();
builder.Services.AddHttpClient(); // Required for NodeChannelClient
builder.Services.AddSingleton<INodeChannelClient, NodeChannelClient>();

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
