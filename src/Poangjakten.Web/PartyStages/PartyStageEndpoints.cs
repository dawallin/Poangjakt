using Poangjakten.Web.Administration;
using Poangjakten.Web.Participants;

namespace Poangjakten.Web.PartyStages;

public static class PartyStageEndpoints
{
    public static IEndpointRouteBuilder MapPartyStageEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/party-stages", (PartyStageRegistry stages) =>
            Results.Ok(stages.List().Select(PublicPartyStageResponse.From)));

        routes.MapGet("/api/participants/{participantId}/table", (
            string participantId,
            ParticipantRegistry participants,
            PartyStageRegistry stages) =>
        {
            var participant = participants.Find(participantId);
            if (participant is null) return Results.NotFound();

            if (!stages.IsUnlocked(PartyStageDefinitions.TableRevealId))
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status423Locked,
                    title: "Borden har inte låsts upp ännu.");
            }

            var table = PartyTables.Find(participant.TableId);
            if (table is null) return Results.NotFound();

            var members = participants.List()
                .Where(member => member.TableId == participant.TableId)
                .OrderBy(member => member.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .Select(member => new TableMemberResponse(
                    member.DisplayName,
                    member.Id == participant.Id))
                .ToArray();

            return Results.Ok(new ParticipantTableResponse(
                table.Number,
                table.Name,
                table.DisplayName,
                members));
        });

        var admin = routes.MapGroup("/api/admin/party-stages");
        admin.AddEndpointFilter<AdminEndpointFilter>();

        admin.MapGet("/", (PartyStageRegistry stages) =>
            Results.Ok(stages.List().Select(AdminPartyStageResponse.From)));

        admin.MapPut("/{id}", async (
            string id,
            SetPartyStageRequest request,
            PartyStageRegistry stages,
            CancellationToken cancellationToken) =>
        {
            var stage = await stages.SetUnlockedAsync(id, request.IsUnlocked, cancellationToken);
            return stage is null
                ? Results.NotFound()
                : Results.Ok(AdminPartyStageResponse.From(stage));
        });

        return routes;
    }
}

public sealed record PublicPartyStageResponse(string Id, bool IsUnlocked)
{
    public static PublicPartyStageResponse From(PartyStageStatus stage) =>
        new(stage.Definition.Id, stage.IsUnlocked);
}

public sealed record AdminPartyStageResponse(
    string Id,
    string DisplayName,
    string Description,
    bool IsUnlocked,
    DateTimeOffset? UpdatedAt)
{
    public static AdminPartyStageResponse From(PartyStageStatus stage) =>
        new(
            stage.Definition.Id,
            stage.Definition.DisplayName,
            stage.Definition.Description,
            stage.IsUnlocked,
            stage.UpdatedAt);
}

public sealed record SetPartyStageRequest(bool IsUnlocked);
public sealed record ParticipantTableResponse(
    int Number,
    string Name,
    string DisplayName,
    IReadOnlyList<TableMemberResponse> Members);
public sealed record TableMemberResponse(string DisplayName, bool IsCurrentParticipant);
