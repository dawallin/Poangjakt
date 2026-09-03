namespace Poangjakten.Web.Administration;

public sealed class AdminEndpointFilter(AdminSessionService sessions) : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        return sessions.IsAuthenticated(context.HttpContext)
            ? next(context)
            : ValueTask.FromResult<object?>(Results.Unauthorized());
    }
}
