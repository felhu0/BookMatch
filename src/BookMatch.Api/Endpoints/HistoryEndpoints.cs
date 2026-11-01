using BookMatch.Api.Services;
using System.Security.Claims;

namespace BookMatch.Api.Endpoints;

public static class HistoryEndpoints
{
    public static IEndpointRouteBuilder MapHistoryEndpoints(this IEndpointRouteBuilder app)
    {
        // Bara inloggade, CSRF-skydd (Cross-Site Request Forgery) på DELETE
        var group = app.MapGroup("/api/history")
            .RequireAuthorization()
            .AddEndpointFilter<AntiforgeryFilter>();

        // Hämta den inloggade användarens senaste konversation (204 om ingen finns)
        group.MapGet("", async (
            ClaimsPrincipal user,
            IConversationService conversationService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

            // Personlig data ska inte sparas i webbläsarens eller proxyns cache
            httpContext.Response.Headers.CacheControl = "no-store";

            var conversation = await conversationService.GetLatestAsync(userId, cancellationToken);
            return conversation is null ? Results.NoContent() : Results.Ok(conversation);
        });

        // Radera den inloggade användarens sparade konversation
        group.MapDelete("", async (
            ClaimsPrincipal user,
            IConversationService conversationService,
            CancellationToken cancellationToken) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            await conversationService.DeleteAsync(userId, cancellationToken);
            return Results.NoContent();
        });

        return app;
    }
}
