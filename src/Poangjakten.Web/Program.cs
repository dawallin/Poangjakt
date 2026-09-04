using Azure.Core;
using Azure.Identity;
using Poangjakten.Web.Administration;
using Poangjakten.Web.Challenges;
using Poangjakten.Web.Participants;
using Poangjakten.Web.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));
builder.Services.AddSingleton<TokenCredential, DefaultAzureCredential>();
builder.Services.AddSingleton<AzureStorageClients>();
builder.Services.AddSingleton<StorageDiagnostics>();
builder.Services.AddSingleton<IParticipantRepository, TableParticipantRepository>();
builder.Services.AddSingleton<ParticipantRegistry>();
builder.Services.AddHostedService(serviceProvider => serviceProvider.GetRequiredService<ParticipantRegistry>());
builder.Services.AddSingleton<IChallengeRepository, TableChallengeRepository>();
builder.Services.AddSingleton<ChallengeRegistry>();
builder.Services.AddHostedService(serviceProvider => serviceProvider.GetRequiredService<ChallengeRegistry>());
builder.Services.Configure<AdminOptions>(builder.Configuration.GetSection("Admin"));
builder.Services.AddSingleton<AdminSessionService>();
builder.Services.AddScoped<AdminEndpointFilter>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/hello", () => Results.Ok(new
{
    message = "Hello from Poängjakten!",
    serverTime = DateTimeOffset.UtcNow
}));

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapPost("/health/storage", async (StorageDiagnostics diagnostics, CancellationToken cancellationToken) =>
{
    var result = await diagnostics.RunAsync(cancellationToken);
    return result.IsHealthy ? Results.Ok(result) : Results.Json(result, statusCode: 503);
}).AddEndpointFilter<AdminEndpointFilter>();

app.MapParticipantEndpoints();
app.MapAdministrationEndpoints();
app.MapChallengeEndpoints();

app.MapFallbackToFile("index.html");

app.Run();
