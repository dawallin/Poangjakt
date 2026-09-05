using Poangjakten.Web.Scoring;

namespace Poangjakten.Web.Participants;

public static class ParticipantEndpoints
{
    public static IEndpointRouteBuilder MapParticipantEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/participants");

        group.MapPost("/login", (
            ParticipantLoginRequest request,
            ParticipantRegistry registry,
            ScoreService scores) =>
        {
            var participant = registry.FindByCode(request.Code);
            return participant is null
                ? Results.Unauthorized()
                : Results.Ok(ParticipantSessionResponse.From(participant, scores.GetScore(participant)));
        });

        group.MapGet("/{id}", (string id, ParticipantRegistry registry, ScoreService scores) =>
        {
            var participant = registry.Find(id);
            return participant is null
                ? Results.NotFound()
                : Results.Ok(ParticipantSessionResponse.From(participant, scores.GetScore(participant)));
        });

        group.MapGet("/", (ParticipantRegistry registry, ScoreService scores) =>
            Results.Ok(registry.List()
                .Select(participant => LeaderboardParticipantResponse.From(participant, scores.GetScore(participant)))
                .OrderByDescending(participant => participant.Score)
                .ThenBy(participant => participant.DisplayName, StringComparer.CurrentCultureIgnoreCase)));

        return routes;
    }
}

public sealed record ParticipantLoginRequest(string? Code);

public sealed record ParticipantSessionResponse(string Id, string DisplayName, int Score, string Clue, bool HasTable)
{
    public static ParticipantSessionResponse From(Participant participant, int score) =>
        new(participant.Id, participant.DisplayName, score, participant.Clue, participant.HasTable);
}

public sealed record LeaderboardParticipantResponse(string Id, string DisplayName, int Score)
{
    public static LeaderboardParticipantResponse From(Participant participant, int score) =>
        new(participant.Id, participant.DisplayName, score);
}
