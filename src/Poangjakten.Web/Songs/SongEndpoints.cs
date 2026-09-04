using Azure;
using Poangjakten.Web.Administration;

namespace Poangjakten.Web.Songs;

public static class SongEndpoints
{
    private const long MaxImageBytes = 6 * 1024 * 1024;

    public static IEndpointRouteBuilder MapSongEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/songs", (SongRegistry songs) =>
            Results.Ok(songs.List().Select(SongResponse.From)));

        routes.MapGet("/api/songs/{id}/image", DownloadImageAsync);

        var admin = routes.MapGroup("/api/admin/songs");
        admin.AddEndpointFilter<AdminEndpointFilter>();

        admin.MapPost("/", async (
            SaveSongRequest request,
            SongRegistry songs,
            CancellationToken cancellationToken) =>
            ToHttpResult(await songs.CreateAsync(
                request.Title,
                request.Melody,
                request.Lyrics,
                request.SortOrder,
                cancellationToken),
                created: true));

        admin.MapPut("/{id}", async (
            string id,
            SaveSongRequest request,
            SongRegistry songs,
            CancellationToken cancellationToken) =>
            ToHttpResult(await songs.UpdateAsync(
                id,
                request.Title,
                request.Melody,
                request.Lyrics,
                request.SortOrder,
                cancellationToken),
                created: false));

        admin.MapPost("/{id}/image", UploadImageAsync);

        admin.MapDelete("/{id}/image", async (
            string id,
            SongService service,
            CancellationToken cancellationToken) =>
            await service.RemoveImageAsync(id, cancellationToken)
                ? Results.NoContent()
                : Results.NotFound());

        admin.MapDelete("/{id}", async (
            string id,
            SongService service,
            CancellationToken cancellationToken) =>
            await service.RemoveAsync(id, cancellationToken)
                ? Results.NoContent()
                : Results.NotFound());

        return routes;
    }

    private static async Task<IResult> UploadImageAsync(
        string id,
        HttpRequest request,
        SongService service,
        CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType)
        {
            return Validation("Välj en bild att ladda upp.");
        }

        var form = await request.ReadFormAsync(cancellationToken);
        var image = form.Files.GetFile("image");
        if (image is null || image.Length is <= 0 or > MaxImageBytes)
        {
            return Validation("Den komprimerade bilden är för stor eller saknas.");
        }

        if (!await IsJpegAsync(image, cancellationToken))
        {
            return Validation("Bilden måste skickas som JPEG.");
        }

        await using var stream = image.OpenReadStream();
        var song = await service.SetImageAsync(id, stream, cancellationToken);
        return song is null ? Results.NotFound() : Results.Ok(SongResponse.From(song));
    }

    private static async Task<IResult> DownloadImageAsync(
        string id,
        HttpContext context,
        SongRegistry songs,
        SongBlobStore blobs,
        CancellationToken cancellationToken)
    {
        var song = songs.Find(id);
        if (song?.ImageBlobName is null)
        {
            return Results.NotFound();
        }

        try
        {
            var download = await blobs.DownloadAsync(song.ImageBlobName, cancellationToken);
            context.Response.Headers.CacheControl = "public, max-age=86400";
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            return Results.Stream(download.Content, "image/jpeg");
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return Results.NotFound();
        }
    }

    private static IResult ToHttpResult(SongMutationResult result, bool created)
    {
        if (result.WasNotFound)
        {
            return Results.NotFound();
        }

        if (result.Song is null)
        {
            return Validation(result.Error ?? "Sången är ogiltig.");
        }

        var response = SongResponse.From(result.Song);
        return created
            ? Results.Created($"/api/admin/songs/{result.Song.Id}", response)
            : Results.Ok(response);
    }

    private static async Task<bool> IsJpegAsync(IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var signature = new byte[3];
        var bytesRead = await stream.ReadAsync(signature, cancellationToken);
        return bytesRead == signature.Length &&
               signature[0] == 0xff && signature[1] == 0xd8 && signature[2] == 0xff;
    }

    private static IResult Validation(string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { ["song"] = [message] });
}

public sealed record SaveSongRequest(string? Title, string? Melody, string? Lyrics, int SortOrder);

public sealed record SongResponse(
    string Id,
    string Title,
    string Melody,
    string Lyrics,
    int SortOrder,
    string? ImageUrl)
{
    public static SongResponse From(Song song) => new(
        song.Id,
        song.Title,
        song.Melody,
        song.Lyrics,
        song.SortOrder,
        song.ImageBlobName is null
            ? null
            : $"/api/songs/{song.Id}/image?v={song.UpdatedAt.ToUnixTimeMilliseconds()}");
}
