using Poangjakten.Web.Participants;

namespace Poangjakten.Web.Administration;

public static class AdministrationEndpoints
{
    public static IEndpointRouteBuilder MapAdministrationEndpoints(this IEndpointRouteBuilder routes)
    {
        // Authentication and an admin authorization policy will be attached to
        // this route group before the application is used at the party.
        var group = routes.MapGroup("/api/admin");

        group.MapGet("/participants", (ParticipantRegistry registry) =>
            Results.Ok(registry.List().Select(ParticipantResponse.From)));

        group.MapDelete("/participants/{id}", async (
            string id,
            ParticipantRegistry registry,
            CancellationToken cancellationToken) =>
        {
            var removed = await registry.RemoveAsync(id, cancellationToken);
            return removed ? Results.NoContent() : Results.NotFound();
        });

        return routes;
    }
}
