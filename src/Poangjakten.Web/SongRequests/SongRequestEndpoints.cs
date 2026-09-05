using Poangjakten.Web.Administration;
using Poangjakten.Web.Participants;
using Poangjakten.Web.PartyStages;

namespace Poangjakten.Web.SongRequests;

public static class SongRequestEndpoints
{
    public static IEndpointRouteBuilder MapSongRequestEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/participants/{participantId}/song-requests", (
            string participantId,
            ParticipantRegistry participants,
            SongRequestRegistry songRequests,
            PartyStageRegistry stages) =>
        {
            var participant = participants.Find(participantId);
            if (participant is null) return Results.NotFound();
            if (!stages.IsUnlocked(PartyStageDefinitions.TableRevealId)) return TablesLocked();

            return Results.Ok(songRequests.List().Select(songRequest =>
                SongRequestResponse.From(songRequest, songRequest.TableId == participant.TableId)));
        });

        routes.MapPost("/api/participants/{participantId}/song-requests", async (
            string participantId,
            SaveSongRequest request,
            ParticipantRegistry participants,
            SongRequestRegistry songRequests,
            PartyStageRegistry stages,
            CancellationToken cancellationToken) =>
        {
            var participant = participants.Find(participantId);
            if (participant is null) return Results.NotFound();
            if (!stages.IsUnlocked(PartyStageDefinitions.TableRevealId)) return TablesLocked();

            var result = await songRequests.CreateAsync(
                request.Artist,
                request.Title,
                participant.TableId,
                participant.Id,
                cancellationToken);
            if (result.SongRequest is null)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["songRequest"] = [result.Error ?? "Låtönskemålet är ogiltigt."]
                });
            }

            return Results.Created(
                $"/api/participants/{participantId}/song-requests/{result.SongRequest.Id}",
                SongRequestResponse.From(result.SongRequest, true));
        });

        routes.MapDelete("/api/participants/{participantId}/song-requests/{songRequestId}", async (
            string participantId,
            string songRequestId,
            ParticipantRegistry participants,
            SongRequestRegistry songRequests,
            PartyStageRegistry stages,
            CancellationToken cancellationToken) =>
        {
            var participant = participants.Find(participantId);
            if (participant is null) return Results.NotFound();
            if (!stages.IsUnlocked(PartyStageDefinitions.TableRevealId)) return TablesLocked();

            var songRequest = songRequests.Find(songRequestId);
            if (songRequest is null) return Results.NotFound();
            if (songRequest.TableId != participant.TableId) return Results.Forbid();

            return await songRequests.RemoveAsync(songRequestId, cancellationToken)
                ? Results.NoContent()
                : Results.NotFound();
        });

        var admin = routes.MapGroup("/api/admin/song-requests");
        admin.AddEndpointFilter<AdminEndpointFilter>();

        admin.MapGet("/", (SongRequestRegistry songRequests) =>
            Results.Ok(songRequests.List().Select(songRequest =>
                SongRequestResponse.From(songRequest, false))));

        admin.MapDelete("/{songRequestId}", async (
            string songRequestId,
            SongRequestRegistry songRequests,
            CancellationToken cancellationToken) =>
            await songRequests.RemoveAsync(songRequestId, cancellationToken)
                ? Results.NoContent()
                : Results.NotFound());

        return routes;
    }

    private static IResult TablesLocked() => Results.Problem(
        statusCode: StatusCodes.Status423Locked,
        title: "Låtlistan låses upp tillsammans med borden.");
}

public sealed record SaveSongRequest(string? Artist, string? Title);

public sealed record SongRequestResponse(
    string Id,
    string Artist,
    string Title,
    int TableNumber,
    string TableName,
    string TableDisplayName,
    bool IsOwnTable,
    DateTimeOffset RequestedAt)
{
    public static SongRequestResponse From(SongRequest songRequest, bool isOwnTable)
    {
        var table = PartyTables.Find(songRequest.TableId);
        return new(
            songRequest.Id,
            songRequest.Artist,
            songRequest.Title,
            table?.Number ?? 0,
            table?.Name ?? "Okänt bord",
            table?.DisplayName ?? "Okänt bord",
            isOwnTable,
            songRequest.RequestedAt);
    }
}
