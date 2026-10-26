using BookMatch.Api.Models;
using Microsoft.Extensions.AI;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace BookMatch.Api.Services;

//Här byggs meddelandelistan (system prompt + användarens meddelande) och skickas via IChatClient.
public class ChatService(IChatClient chatClient) : IChatService
{
    // Tillfällig system prompt (instruktioner till AI:n), ersätts av PromptBuilder i säkerhetssteget
    // Varje regel motverkar ett fel som setts i hallucinationstesterna
    private const string SystemPrompt = """
        You are BookMatch, an assistant that only gives book recommendations
        and information about how books have been received by readers and critics.

        Language and format:
        - Always give the original title. Add the translated title in parentheses only if you are certain it is the published title.
        - Answer in plain text only, without Markdown formatting.
        - Use correct spelling and grammar in the answer language.

        Choosing books:
        - Only recommend books that clearly belong to the requested genre and suit the reading level.
        - Only recommend books you are highly confident exist, with the correct title and author.
        - Prefer well-known standalone books or the first book of a well-known series.
        - Do not state the order of a book in a series unless you are certain.
        - Use the translated title only if you are certain it is the published title. Otherwise use the original title.
        - If you cannot find three books you are confident about, recommend fewer and say so.
        - Only recommend books that are written in or published in translation in the requested language.

        Describing books:
        - For each book, give the title, the author and one or two general sentences about the book.
        - Do not invent character names, places, plot details, awards, sales figures or quotes.
        - Describe reception in general terms only.
        - If you are unsure about something, say so instead of guessing.

        Follow-up questions:
        - The user may ask follow-up questions about the recommended books, other books, or how books have been received.
        - If a question is not about books, politely say that you can only help with books.
        - Follow the same rules about accuracy in every answer.
     
        """;
    public async IAsyncEnumerable<string> StreamReplyAsync(
        ChatRequest request, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
       
        // Meddelandelista: system prompt, första meddelandet, historik och ny följdfråga
        var messages = BuildMessages(request);

        // Strömmande AI-anrop via AI-klienten (IChatClient), varje textbit skickas vidare direkt
        await foreach (var update in chatClient.GetStreamingResponseAsync(messages, cancellationToken: cancellationToken))
        {
            if (!string.IsNullOrEmpty(update.Text))
            {
                yield return update.Text; // skicka en bit, pausa, fortsätt med nästa
            }
        }
    }

    // Bygger hela meddelandelistan som skickas till AI:n
    private static List<ChatMessage> BuildMessages(ChatRequest request)
    {
        List<ChatMessage> messages = [
                new(ChatRole.System, SystemPrompt),
                new(ChatRole.User, BuildFirstMessage(request))
            ];

        // Historik från webbläsaren
        foreach (var message in request.History ?? [])
        {
            var role = message.Role == ChatMessageRole.Assistant ? ChatRole.Assistant : ChatRole.User;
            messages.Add(new ChatMessage(role, message.Content));
        }

        // Ny följdfråga (fritext från användaren)
        if (request.Message is not null)
        {
            messages.Add(new ChatMessage(ChatRole.User, request.Message));
        }

        return messages;
    }

    // Första meddelandet byggs av servern från formulärets val (strukturerad input)
    private static string BuildFirstMessage(ChatRequest request)
    {
        // Språknamn från språkkod (ISO 639-1, tvåbokstavskod), t.ex. "sv" → "Swedish"
        var languageName = new CultureInfo(request.Language).EnglishName;

        return $"Recommend up to 3 books. " +
               $"Language: {languageName}. " +
               $"Genre: {request.Genre}. " +
               $"Reading level: {DescribeLevel(request.Level)}.";
    }

    // Beskrivning av läsnivå så att AI:n tolkar nivån likadant varje gång
    private static string DescribeLevel(ReadingLevel level) => level switch
    {
        ReadingLevel.Easy => "Easy (short books with simple language, suitable for young readers or beginners in the language)",
        ReadingLevel.Medium => "Medium (books of normal length with everyday language, suitable for teenagers and adults)",
        ReadingLevel.Advanced => "Advanced (longer or literary books with complex language or structure)",
        _ => throw new ArgumentOutOfRangeException(nameof(level), "Unsupported reading level.")
    };
}
