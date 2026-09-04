namespace Poangjakten.Web.Participants;

using Poangjakten.Web.Scoring;

public static class ParticipantEndpoints
{
    public static IEndpointRouteBuilder MapParticipantEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/participants");

        group.MapPost("/register", async (
            RegisterParticipantRequest request,
            ParticipantRegistry registry,
            ScoreService scores,
            CancellationToken cancellationToken) =>
        {
            var result = await registry.RegisterAsync(request.DisplayName, cancellationToken);
            if (result.Participant is null)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(request.DisplayName)] = [result.Error ?? "Namnet är ogiltigt."]
                });
            }

            var response = ParticipantResponse.From(result.Participant, scores.GetScore(result.Participant));
            return result.WasCreated
                ? Results.Created($"/api/participants/{result.Participant.Id}", response)
                : Results.Ok(response);
        });

        group.MapGet("/{id}", (string id, ParticipantRegistry registry, ScoreService scores) =>
        {
            var participant = registry.Find(id);
            return participant is null
                ? Results.NotFound()
                : Results.Ok(ParticipantResponse.From(participant, scores.GetScore(participant)));
        });

        group.MapGet("/", (ParticipantRegistry registry, ScoreService scores) =>
            Results.Ok(registry.List()
                .Select(participant => ParticipantResponse.From(participant, scores.GetScore(participant)))
                .OrderByDescending(participant => participant.Score)
                .ThenBy(participant => participant.DisplayName, StringComparer.CurrentCultureIgnoreCase)));

        return routes;
    }
}

public sealed record RegisterParticipantRequest(string? DisplayName);

public sealed record ParticipantResponse(string Id, string DisplayName, int Score)
{
    public static ParticipantResponse From(Participant participant, int score) =>
        new(participant.Id, participant.DisplayName, score);
}
