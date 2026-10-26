using BookMatch.Api.Models;

namespace BookMatch.Api.Validation;

public static class ChatRequestValidator
{
    public const int LanguageCodeLength = 2;
    public const int MaxMessageLength = 500;
    public const int MaxAssistantMessageLength = 5000;

    // Maxantal meddelanden i historiken (begränsar kostnad och storlek på anrop till AI:n)
    public const int MaxHistoryCount = 20;

    // Returnerar valideringsfel per fält, tom lista = giltigt anrop
    public static Dictionary<string, string[]> Validate(ChatRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrEmpty(request.Language)
            || request.Language.Length != LanguageCodeLength
            || !LanguageCodes.Allowed.Contains(request.Language))
        {
            errors["language"] = ["Language must be a supported ISO 639-1 code, e.g. 'sv'."];
        }

        // Genre och nivå: extra kontroll att värdet finns i enum 
        if (!Enum.IsDefined(request.Genre))
        {
            errors["genre"] = ["Genre is not supported."];
        }

        if (!Enum.IsDefined(request.Level))
        {
            errors["level"] = ["Reading level is not supported."];
        }

        IReadOnlyList<ChatMessageDto> history = request.History ?? [];

        // Följdfråga: krävs om historik finns, maxlängd
        if (request.Message is null)
        {
            if (history.Count > 0)
            {
                errors["message"] = ["A follow-up message is required when history is sent."];
            }
        }
        else if (string.IsNullOrWhiteSpace(request.Message) || request.Message.Length > MaxMessageLength)
        {
            errors["message"] = [$"Message must be between 1 and {MaxMessageLength} characters."];
        }

        // Historik: maxantal och rätt form (bara kontrolleras vid följdfråga)
        if (request.Message is not null)
        {
            if (history.Count > MaxHistoryCount)
            {
                errors["history"] = [$"History can contain at most {MaxHistoryCount} messages."];
            }
            else if (!IsValidHistory(history))
            {
                errors["history"] = ["History has an invalid format."];
            }
        }

        return errors;
    }

    // Historiken måste växla Assistant → User → Assistant ... och sluta med Assistant
    private static bool IsValidHistory(IReadOnlyList<ChatMessageDto> history)
    {
        // Minst AI:ns första svar, och udda antal (slutar med ett svar från AI:n)
        if (history.Count == 0 || history.Count % 2 == 0)
        {
            return false;
        }

        for (var i = 0; i < history.Count; i++)
        {
            var message = history[i];
            var expectedRole = i % 2 == 0 ? ChatMessageRole.Assistant : ChatMessageRole.User;

            if (message is null || message.Role != expectedRole || string.IsNullOrWhiteSpace(message.Content))
            {
                return false;
            }

            var maxLength = message.Role == ChatMessageRole.Assistant
                ? MaxAssistantMessageLength
                : MaxMessageLength;

            if (message.Content.Length > maxLength)
            {
                return false;
            }
        }

        return true;
    }
}
