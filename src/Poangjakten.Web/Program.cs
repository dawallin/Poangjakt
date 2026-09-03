using Azure.Core;
using Azure.Identity;
using Poangjakten.Web.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));
builder.Services.AddSingleton<TokenCredential, DefaultAzureCredential>();
builder.Services.AddSingleton<StorageDiagnostics>();

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
});

app.MapFallbackToFile("index.html");

app.Run();
