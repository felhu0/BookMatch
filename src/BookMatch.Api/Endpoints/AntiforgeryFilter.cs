using Microsoft.AspNetCore.Antiforgery;

namespace BookMatch.Api.Endpoints;

// Endpoint-filter: kontrollerar CSRF-token (Cross-Site Request Forgery) innan endpointen körs
// antiforgery = ASP.NET Cores inbyggda tjänst (IAntiforgery) som skapar och kontrollerar token, fås via DI (Dependency Injection)
public class AntiforgeryFilter(IAntiforgery antiforgery) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        // Kontrollerar att request-token (X-CSRF-TOKEN) och cookie-token hör ihop (GET-anrop godkänns automatiskt)
        if (!await antiforgery.IsRequestValidAsync(context.HttpContext))  // läser från httpContext
        {
            return Results.Problem(
                detail: "Invalid or missing antiforgery token.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        return await next(context);
    }
}
