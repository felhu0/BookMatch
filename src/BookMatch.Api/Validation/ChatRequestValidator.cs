using BookMatch.Api.Models;

namespace BookMatch.Api.Validation;

public static class ChatRequestValidator
{
    // Maxlängd för språkkod (ISO 639-1 är alltid två bokstäver)
    public const int LanguageCodeLength = 2;

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

        return errors;
    }
}
