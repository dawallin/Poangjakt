using Poangjakten.Web.Participants;

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

public sealed record AdminLoginRequest(string? Secret);
public sealed record AdminSessionResponse(bool IsAdmin, string DisplayName);
