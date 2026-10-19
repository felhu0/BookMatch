using BookMatch.Api.Models;
using Microsoft.Extensions.AI;

namespace BookMatch.Api.Services;

//Här byggs meddelandelistan (system prompt + användarens meddelande) och skickas via IChatClient.
public class ChatService(IChatClient chatClient) : IChatService
{
    // Tillfällig system prompt (instruktioner till AI:n), ersätts av PromptBuilder i säkerhetssteget
    private const string SystemPrompt =
        "You are BookMatch, an assistant that only gives book recommendations " +
        "and information about how books have been received. " +
        "Always answer in the language given by the ISO 639-1 language code. " +
        "Only recommend well-known books that you are sure exist. " +
        "If you are not sure a book exists, say so instead of guessing.";
    public async Task<string> GetReplyAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        // Meddelandelista: system prompt först, sedan meddelandet byggt från formuläret
        //ChatRole.System och ChatRole.User separat: AI:n skiljer på systems instruktioner och användarens text
        List<ChatMessage> messages = [
            new(ChatRole.System, SystemPrompt),
            new(ChatRole.User, BuildFirstMessage(request))
        ];

        // AI-anrop via AI-klienten (IChatClient)
        var response = await chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);

        return response.Text;
    }

    // Första meddelandet byggs av servern från formulärets val (strukturerad input)
    private static string BuildFirstMessage(ChatRequest request) =>
        $"Recommend 3 books. " +
        $"Language (ISO 639-1): {request.Language}. " +
        $"Genre: {request.Genre}. " +
        $"Reading level: {request.Level}.";
}
