using Microsoft.AspNetCore.Antiforgery;

namespace BookMatch.Api.Endpoints;

public static class AntiforgeryEndpoints
{
    public static IEndpointRouteBuilder MapAntiforgeryEndpoints(this IEndpointRouteBuilder app)
    {
        // Delar ut CSRF-token (Cross-Site Request Forgery) till frontend, ingen kontroll sker här
        // (kontrollen sker vid POST i AntiforgeryFilter: cookie-token och request-token måste höra ihop)
        app.MapGet("/api/antiforgery/token", (IAntiforgery antiforgery, HttpContext httpContext) =>
        {
            // Ramverket skapar token-paret, cookie-token skickas i Set-Cookie (sparas automatiskt av webbläsaren)
            var tokens = antiforgery.GetAndStoreTokens(httpContext);  // läser och skriver i httpContext

            // Request-token skickas i body (frontend skickar tillbaka den i X-CSRF-TOKEN)
            return Results.Ok(new { requestToken = tokens.RequestToken }); 
        });

        return app;
    }
}
