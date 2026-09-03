namespace Poangjakten.Web.Participants;

public static class ParticipantEndpoints
{
    public static IEndpointRouteBuilder MapParticipantEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/participants");

        group.MapPost("/register", async (
            RegisterParticipantRequest request,
            ParticipantRegistry registry,
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

            var response = ParticipantResponse.From(result.Participant);
            return result.WasCreated
                ? Results.Created($"/api/participants/{result.Participant.Id}", response)
                : Results.Ok(response);
        });

        group.MapGet("/{id}", (string id, ParticipantRegistry registry) =>
        {
            var participant = registry.Find(id);
            return participant is null
                ? Results.NotFound()
                : Results.Ok(ParticipantResponse.From(participant));
        });

        group.MapGet("/", (ParticipantRegistry registry) =>
            Results.Ok(registry.List().Select(ParticipantResponse.From)));

        return routes;
    }
}

public sealed record RegisterParticipantRequest(string? DisplayName);

public sealed record ParticipantResponse(string Id, string DisplayName, int Score)
{
    public static ParticipantResponse From(Participant participant) =>
        new(participant.Id, participant.DisplayName, participant.Score);
}
