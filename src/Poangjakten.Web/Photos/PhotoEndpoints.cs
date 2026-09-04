using Azure;
using Poangjakten.Web.Administration;

namespace Poangjakten.Web.Photos;

public static class PhotoEndpoints
{
    private const long MaxImageBytes = 6 * 1024 * 1024;
    private const long MaxThumbnailBytes = 1024 * 1024;

    public static IEndpointRouteBuilder MapPhotoEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/photos", (PhotoRegistry photos) =>
            Results.Ok(photos.List().Select(PhotoResponse.From)));

        routes.MapPost("/api/photos", UploadAsync);

        routes.MapGet("/api/photos/{id}/image", (
            string id,
            HttpContext context,
            PhotoRegistry photos,
            PhotoBlobStore blobs,
            CancellationToken cancellationToken) =>
            DownloadAsync(id, thumbnail: false, context, photos, blobs, cancellationToken));

        routes.MapGet("/api/photos/{id}/thumbnail", (
            string id,
            HttpContext context,
            PhotoRegistry photos,
            PhotoBlobStore blobs,
            CancellationToken cancellationToken) =>
            DownloadAsync(id, thumbnail: true, context, photos, blobs, cancellationToken));

        routes.MapDelete("/api/admin/photos/{id}", async (
            string id,
            PhotoService service,
            CancellationToken cancellationToken) =>
        {
            var removed = await service.DeleteAsync(id, cancellationToken);
            return removed ? Results.NoContent() : Results.NotFound();
        }).AddEndpointFilter<AdminEndpointFilter>();

        return routes;
    }

    private static async Task<IResult> UploadAsync(
        HttpRequest request,
        PhotoService service,
        CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType)
        {
            return Validation("Välj en bild att ladda upp.");
        }

        var form = await request.ReadFormAsync(cancellationToken);
        var participantId = form["participantId"].ToString();
        var image = form.Files.GetFile("image");
        var thumbnail = form.Files.GetFile("thumbnail");

        if (string.IsNullOrWhiteSpace(participantId))
        {
            return Validation("Deltagaren saknas. Logga in igen.");
        }

        if (image is null || image.Length is <= 0 or > MaxImageBytes ||
            thumbnail is null || thumbnail.Length is <= 0 or > MaxThumbnailBytes)
        {
            return Validation("Den komprimerade bilden eller tumnageln är för stor eller saknas.");
        }

        if (!await IsJpegAsync(image, cancellationToken) ||
            !await IsJpegAsync(thumbnail, cancellationToken))
        {
            return Validation("Bilderna måste skickas som JPEG.");
        }

        await using var imageStream = image.OpenReadStream();
        await using var thumbnailStream = thumbnail.OpenReadStream();
        var result = await service.UploadAsync(
            participantId,
            imageStream,
            image.Length,
            thumbnailStream,
            thumbnail.Length,
            cancellationToken);

        if (result.Photo is null)
        {
            return Validation(result.Error ?? "Bilden kunde inte sparas.");
        }

        return Results.Created($"/api/photos/{result.Photo.Id}", PhotoResponse.From(result.Photo));
    }

    private static async Task<IResult> DownloadAsync(
        string id,
        bool thumbnail,
        HttpContext context,
        PhotoRegistry photos,
        PhotoBlobStore blobs,
        CancellationToken cancellationToken)
    {
        var photo = photos.Find(id);
        if (photo is null)
        {
            return Results.NotFound();
        }

        try
        {
            var download = await blobs.DownloadAsync(
                thumbnail ? photo.ThumbnailBlobName : photo.ImageBlobName,
                cancellationToken);
            context.Response.Headers.CacheControl = "public, max-age=86400";
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            return Results.Stream(
                download.Content,
                "image/jpeg");
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return Results.NotFound();
        }
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
        Results.ValidationProblem(new Dictionary<string, string[]> { ["photo"] = [message] });
}

public sealed record PhotoResponse(
    string Id,
    string ParticipantId,
    string PhotographerDisplayName,
    string ImageUrl,
    string ThumbnailUrl,
    DateTimeOffset UploadedAt)
{
    public static PhotoResponse From(Photo photo) => new(
        photo.Id,
        photo.ParticipantId,
        photo.PhotographerDisplayName,
        $"/api/photos/{photo.Id}/image",
        $"/api/photos/{photo.Id}/thumbnail",
        photo.UploadedAt);
}
