using BookMatch.Api.Models;
using BookMatch.Api.Services;
using BookMatch.Api.Validation;
using System.ClientModel;
using System.Text.Json;

namespace BookMatch.Api.Endpoints;

public static class ChatEndpoints
{
    public static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/chat", async (
            ChatRequest request,
            IChatService chatService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            // Validering med vitlista och maxlängd, innan något skickas till AI:n
            var errors = ChatRequestValidator.Validate(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            // SSE (Server-Sent Events): svaret skickas som en ström av händelser
            var response = httpContext.Response;
            response.ContentType = "text/event-stream"; //Markerar svaret som en SSE-ström
            response.Headers.CacheControl = "no-cache"; //Förhindrar cachning och buffring

            try
            {
                await foreach (var text in chatService.StreamReplyAsync(request, cancellationToken))
                {
                    await WriteEventAsync(response, "message", new { text }, cancellationToken);
                }

                // Signal till frontend att svaret är klart
                await WriteEventAsync(response, "done", new { }, cancellationToken);
            }
            // Gratisgränsen hos Gemini nådd (HTTP 429, Too Many Requests)
            catch (ClientResultException ex) when (ex.Status == 429)
            {
                await WriteEventAsync(response, "error", new { code = "busy" }, cancellationToken);
            }

            // Övriga fel från Gemini, inga interna detaljer skickas till användaren
            catch (ClientResultException)
            {
                await WriteEventAsync(response, "error", new { code = "unavailable" }, cancellationToken);
            }

            return Results.Empty;
        }).AddEndpointFilter<AntiforgeryFilter>();

        return app;
    }

    // Skriver en SSE-händelse (Server-Sent Events) och skickar den direkt till webbläsaren
    private static async Task WriteEventAsync(HttpResponse response, string eventName, object data, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(data);
        await response.WriteAsync($"event: {eventName}\ndata: {json}\n\n", cancellationToken);
        await response.Body.FlushAsync(cancellationToken);
    }
}
