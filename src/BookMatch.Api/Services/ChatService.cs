using BookMatch.Api.Models;
using Microsoft.Extensions.AI;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace BookMatch.Api.Services;

// Chatt-tjänst: skickar meddelandelistan från prompt-byggaren och strömmar svaret via AI-klienten (IChatClient)
public class ChatService(IChatClient chatClient, IPromptBuilder promptBuilder) : IChatService
{
    public async IAsyncEnumerable<string> StreamReplyAsync(
        ChatRequest request, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {

        // Meddelandelista med säkerhetsregler och avgränsad användartext
        var messages = promptBuilder.BuildMessages(request);

        // Strömmande AI-anrop, varje textbit skickas vidare direkt
        await foreach (var update in chatClient.GetStreamingResponseAsync(messages, cancellationToken: cancellationToken))
        {
            if (!string.IsNullOrEmpty(update.Text))
            {
                yield return update.Text; // skicka en bit, pausa, fortsätt med nästa
            }
        }
    }
}
