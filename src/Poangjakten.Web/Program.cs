using Azure.Core;
using Azure.Identity;
using Poangjakten.Web.Administration;
using Poangjakten.Web.Challenges;
using Poangjakten.Web.Participants;
using Poangjakten.Web.Photos;
using Poangjakten.Web.Scoring;
using Poangjakten.Web.Songs;
using Poangjakten.Web.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
    options.MultipartBodyLengthLimit = 8 * 1024 * 1024);
builder.Services.AddSingleton<TokenCredential, DefaultAzureCredential>();
builder.Services.AddSingleton<AzureStorageClients>();
builder.Services.AddSingleton<StorageDiagnostics>();
builder.Services.AddSingleton<IParticipantRepository, TableParticipantRepository>();
builder.Services.AddSingleton<ParticipantRegistry>();
builder.Services.AddHostedService(serviceProvider => serviceProvider.GetRequiredService<ParticipantRegistry>());
builder.Services.AddSingleton<IChallengeRepository, TableChallengeRepository>();
builder.Services.AddSingleton<ChallengeRegistry>();
builder.Services.AddHostedService(serviceProvider => serviceProvider.GetRequiredService<ChallengeRegistry>());
builder.Services.AddSingleton<IChallengeCompletionRepository, TableChallengeCompletionRepository>();
builder.Services.AddSingleton<ChallengeCompletionRegistry>();
builder.Services.AddHostedService(serviceProvider => serviceProvider.GetRequiredService<ChallengeCompletionRegistry>());
builder.Services.AddSingleton<ScoreService>();
builder.Services.AddSingleton<IPhotoRepository, TablePhotoRepository>();
builder.Services.AddSingleton<PhotoRegistry>();
builder.Services.AddHostedService(serviceProvider => serviceProvider.GetRequiredService<PhotoRegistry>());
builder.Services.AddSingleton<PhotoBlobStore>();
builder.Services.AddSingleton<PhotoService>();
builder.Services.AddSingleton<ISongRepository, TableSongRepository>();
builder.Services.AddSingleton<SongRegistry>();
builder.Services.AddHostedService(serviceProvider => serviceProvider.GetRequiredService<SongRegistry>());
builder.Services.AddSingleton<SongBlobStore>();
builder.Services.AddSingleton<SongService>();
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
app.MapPhotoEndpoints();
app.MapSongEndpoints();

app.MapFallbackToFile("index.html");

app.Run();
