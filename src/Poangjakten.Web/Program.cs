var builder = WebApplication.CreateBuilder(args);

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

app.MapFallbackToFile("index.html");

app.Run();

