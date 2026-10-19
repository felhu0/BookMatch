using BookMatch.Api.Models;
using BookMatch.Api.Services;
using BookMatch.Api.Validation;
using System.ClientModel;

namespace BookMatch.Api.Endpoints;

public static class ChatEndpoints
{
    public static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/chat", async (ChatRequest request, IChatService chatService, CancellationToken cancellationToken) =>
        {
            // Validering med vitlista och maxlängd, innan något skickas till AI:n
            var errors = ChatRequestValidator.Validate(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            try
            {
                var reply = await chatService.GetReplyAsync(request, cancellationToken);
                return Results.Ok(new ChatReply(reply));
            }
            // Gratisgränsen hos Gemini nådd (HTTP 429, Too Many Requests)
            catch (ClientResultException ex) when (ex.Status == 429)
            {
                return Results.Problem(
                    detail: "The AI service is busy. Please try again later.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            // Övriga fel från Gemini, inga interna detaljer skickas till användaren
            catch (ClientResultException)
            {
                return Results.Problem(
                    detail: "The AI service is currently unavailable.",
                    statusCode: StatusCodes.Status502BadGateway);
            }
        });

        return app;
    }
}
