using Poangjakten.Web.Challenges;
using Poangjakten.Web.Participants;
using Poangjakten.Web.Scoring;

namespace Poangjakten.Web.Administration;

public static class AdministrationEndpoints
{
    public static IEndpointRouteBuilder MapAdministrationEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/api/admin-session", (
            AdminLoginRequest request,
            HttpContext context,
            AdminSessionService sessions) =>
        {
            if (!sessions.TrySignIn(request.Secret, out var session) || session is null)
            {
                return Results.Unauthorized();
            }

            context.Response.Cookies.Append(AdminSessionService.CookieName, session.Token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = session.ExpiresAt,
                IsEssential = true
            });
            return Results.Ok(new AdminSessionResponse(true, session.DisplayName));
        });

        routes.MapGet("/api/admin-session", (HttpContext context, AdminSessionService sessions) =>
            sessions.IsAuthenticated(context)
                ? Results.Ok(new AdminSessionResponse(true, sessions.DisplayName))
                : Results.Unauthorized());

        routes.MapDelete("/api/admin-session", (HttpContext context, AdminSessionService sessions) =>
        {
            sessions.SignOut(context);
            context.Response.Cookies.Delete(AdminSessionService.CookieName);
            return Results.NoContent();
        });

        var group = routes.MapGroup("/api/admin");
        group.AddEndpointFilter<AdminEndpointFilter>();

        group.MapGet("/party-tables", () => Results.Ok(PartyTables.All.Select(PartyTableResponse.From)));

        group.MapGet("/participants", (ParticipantRegistry registry, ScoreService scores) =>
            Results.Ok(registry.List().Select(participant =>
                AdminParticipantResponse.From(participant, scores.GetScore(participant)))));

        group.MapPost("/participants", async (
            SaveParticipantRequest request,
            ParticipantRegistry registry,
            ScoreService scores,
            CancellationToken cancellationToken) =>
        {
            var result = await registry.CreateAsync(
                request.DisplayName, request.LoginCode, request.Clue, request.TableId, cancellationToken);
            if (result.Participant is null) return ParticipantError(result);

            return Results.Created(
                $"/api/admin/participants/{result.Participant.Id}",
                AdminParticipantResponse.From(result.Participant, scores.GetScore(result.Participant)));
        });

        group.MapPut("/participants/{id}", async (
            string id,
            SaveParticipantRequest request,
            ParticipantRegistry registry,
            ScoreService scores,
            CancellationToken cancellationToken) =>
        {
            var result = await registry.UpdateAsync(
                id, request.DisplayName, request.LoginCode, request.Clue, request.TableId, cancellationToken);
            if (result.Participant is null) return ParticipantError(result);

            return Results.Ok(AdminParticipantResponse.From(result.Participant, scores.GetScore(result.Participant)));
        });

        group.MapDelete("/participants/{id}", async (
            string id,
            ParticipantRegistry registry,
            ChallengeCompletionRegistry completions,
            CancellationToken cancellationToken) =>
        {
            if (registry.Find(id) is null) return Results.NotFound();
            await completions.RemoveParticipantAsync(id, cancellationToken);
            var removed = await registry.RemoveAsync(id, cancellationToken);
            return removed ? Results.NoContent() : Results.NotFound();
        });

        return routes;
    }

    private static IResult ParticipantError(ParticipantMutationResult result)
    {
        if (result.WasNotFound) return Results.NotFound();
        if (result.WasConflict) return Results.Conflict(new { title = result.Error });
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["participant"] = [result.Error ?? "Deltagaren är ogiltig."]
        });
    }
}

public sealed record AdminLoginRequest(string? Secret);
public sealed record AdminSessionResponse(bool IsAdmin, string DisplayName);
public sealed record SaveParticipantRequest(string? DisplayName, string? LoginCode, string? Clue, string? TableId);

public sealed record PartyTableResponse(string Id, int Number, string Name, string DisplayName)
{
    public static PartyTableResponse From(PartyTable table) =>
        new(table.Id, table.Number, table.Name, table.DisplayName);
}

public sealed record AdminParticipantResponse(
    string Id,
    string DisplayName,
    string LoginCode,
    string Clue,
    string TableId,
    string TableName,
    int Score)
{
    public static AdminParticipantResponse From(Participant participant, int score)
    {
        var table = PartyTables.Find(participant.TableId);
        return new(
            participant.Id,
            participant.DisplayName,
            participant.LoginCode,
            participant.Clue,
            participant.TableId,
            table?.DisplayName ?? "Ej tilldelad",
            score);
    }
}
