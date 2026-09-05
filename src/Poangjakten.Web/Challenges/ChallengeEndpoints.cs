using Poangjakten.Web.Administration;
using Poangjakten.Web.Participants;
using Poangjakten.Web.PartyStages;
using Poangjakten.Web.Scoring;

namespace Poangjakten.Web.Challenges;

public static class ChallengeEndpoints
{
    public static IEndpointRouteBuilder MapChallengeEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/challenges", (ChallengeRegistry registry) =>
            Results.Ok(registry.List()
                .Where(challenge => challenge.Scope == ChallengeScopes.Individual)
                .Select(ChallengeResponse.From)));

        routes.MapGet("/api/participants/{participantId}/challenges", (
            string participantId,
            ParticipantRegistry participants,
            ChallengeRegistry challenges,
            ChallengeCompletionRegistry completions) =>
        {
            if (participants.Find(participantId) is null) return Results.NotFound();
            var completedIds = completions.CompletedChallengeIds(
                ChallengeCompletionOwners.ForParticipant(participantId));
            return Results.Ok(challenges.List()
                .Where(challenge => challenge.Scope == ChallengeScopes.Individual)
                .Select(challenge => ParticipantChallengeResponse.From(
                    challenge,
                    completedIds.Contains(challenge.Id))));
        });

        routes.MapPut("/api/participants/{participantId}/challenges/{challengeId}", async (
            string participantId,
            string challengeId,
            SetChallengeCompletionRequest request,
            ParticipantRegistry participants,
            ChallengeRegistry challenges,
            ChallengeCompletionRegistry completions,
            ScoreService scores,
            CancellationToken cancellationToken) =>
        {
            var participant = participants.Find(participantId);
            var challenge = challenges.Find(challengeId);
            if (participant is null || challenge?.Scope != ChallengeScopes.Individual) return Results.NotFound();

            await completions.SetAsync(
                ChallengeCompletionOwners.ForParticipant(participantId),
                challengeId,
                request.IsCompleted,
                cancellationToken);
            return Results.Ok(new ChallengeCompletionResponse(
                challengeId,
                request.IsCompleted,
                scores.GetScore(participant)));
        });

        routes.MapGet("/api/participants/{participantId}/table-challenges", (
            string participantId,
            ParticipantRegistry participants,
            ChallengeRegistry challenges,
            ChallengeCompletionRegistry completions,
            PartyStageRegistry stages) =>
        {
            var participant = participants.Find(participantId);
            if (participant is null) return Results.NotFound();
            if (!participant.HasTable) return Results.NotFound();
            if (!stages.IsUnlocked(PartyStageDefinitions.TableRevealId)) return TablesLocked();

            var completedIds = completions.CompletedChallengeIds(
                ChallengeCompletionOwners.ForTable(participant.TableId));
            return Results.Ok(challenges.List()
                .Where(challenge => challenge.Scope == ChallengeScopes.Table)
                .Select(challenge => ParticipantChallengeResponse.From(
                    challenge,
                    completedIds.Contains(challenge.Id))));
        });

        routes.MapPut("/api/participants/{participantId}/table-challenges/{challengeId}", async (
            string participantId,
            string challengeId,
            SetChallengeCompletionRequest request,
            ParticipantRegistry participants,
            ChallengeRegistry challenges,
            ChallengeCompletionRegistry completions,
            PartyStageRegistry stages,
            ScoreService scores,
            CancellationToken cancellationToken) =>
        {
            var participant = participants.Find(participantId);
            var challenge = challenges.Find(challengeId);
            if (participant is null || challenge?.Scope != ChallengeScopes.Table) return Results.NotFound();
            if (!participant.HasTable) return Results.NotFound();
            if (!stages.IsUnlocked(PartyStageDefinitions.TableRevealId)) return TablesLocked();

            await completions.SetAsync(
                ChallengeCompletionOwners.ForTable(participant.TableId),
                challengeId,
                request.IsCompleted,
                cancellationToken);
            return Results.Ok(new ChallengeCompletionResponse(
                challengeId,
                request.IsCompleted,
                scores.GetTableScore(participant.TableId)));
        });

        routes.MapGet("/api/participants/{participantId}/table-leaderboard", (
            string participantId,
            ParticipantRegistry participants,
            PartyStageRegistry stages,
            ScoreService scores) =>
        {
            var participant = participants.Find(participantId);
            if (participant is null) return Results.NotFound();
            if (!participant.HasTable) return Results.NotFound();
            if (!stages.IsUnlocked(PartyStageDefinitions.TableRevealId)) return TablesLocked();

            var leaderboard = PartyTables.All
                .Select(table => new TableLeaderboardResponse(
                    table.Number,
                    table.Name,
                    table.DisplayName,
                    scores.GetTableScore(table.Id),
                    table.Id == participant.TableId))
                .OrderByDescending(table => table.Score)
                .ThenBy(table => table.Number);
            return Results.Ok(leaderboard);
        });

        var admin = routes.MapGroup("/api/admin/challenges");
        admin.AddEndpointFilter<AdminEndpointFilter>();

        admin.MapGet("/", (ChallengeRegistry registry) =>
            Results.Ok(registry.List().Select(ChallengeResponse.From)));

        admin.MapPost("/", async (
            SaveChallengeRequest request,
            ChallengeRegistry registry,
            CancellationToken cancellationToken) =>
        {
            var result = await registry.CreateAsync(
                request.Description,
                request.Points,
                request.Scope,
                cancellationToken);
            return ToHttpResult(result, created: true);
        });

        admin.MapPut("/{id}", async (
            string id,
            SaveChallengeRequest request,
            ChallengeRegistry registry,
            CancellationToken cancellationToken) =>
        {
            var result = await registry.UpdateAsync(
                id,
                request.Description,
                request.Points,
                request.Scope,
                cancellationToken);
            return ToHttpResult(result, created: false);
        });

        admin.MapDelete("/{id}", async (
            string id,
            ChallengeRegistry registry,
            CancellationToken cancellationToken) =>
        {
            var removed = await registry.RemoveAsync(id, cancellationToken);
            return removed ? Results.NoContent() : Results.NotFound();
        });

        return routes;
    }

    private static IResult TablesLocked() => Results.Problem(
        statusCode: StatusCodes.Status423Locked,
        title: "Bordsfunktionerna har inte låsts upp ännu.");

    private static IResult ToHttpResult(ChallengeMutationResult result, bool created)
    {
        if (result.WasNotFound)
        {
            return Results.NotFound();
        }

        if (result.Challenge is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["challenge"] = [result.Error ?? "Uppgiften är ogiltig."]
            });
        }

        var response = ChallengeResponse.From(result.Challenge);
        return created
            ? Results.Created($"/api/admin/challenges/{result.Challenge.Id}", response)
            : Results.Ok(response);
    }
}

public sealed record SaveChallengeRequest(string? Description, int Points, string? Scope);

public sealed record ChallengeResponse(string Id, string Description, int Points, string Scope)
{
    public static ChallengeResponse From(Challenge challenge) =>
        new(challenge.Id, challenge.Description, challenge.Points, challenge.Scope);
}

public sealed record ParticipantChallengeResponse(string Id, string Description, int Points, bool IsCompleted)
{
    public static ParticipantChallengeResponse From(Challenge challenge, bool isCompleted) =>
        new(challenge.Id, challenge.Description, challenge.Points, isCompleted);
}

public sealed record SetChallengeCompletionRequest(bool IsCompleted);
public sealed record ChallengeCompletionResponse(string ChallengeId, bool IsCompleted, int Score);
public sealed record TableLeaderboardResponse(
    int Number,
    string Name,
    string DisplayName,
    int Score,
    bool IsCurrentTable);
