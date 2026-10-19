namespace BookMatch.Api.Validation;

// Tillåtna språkkoder enligt ISO 639-1 
public static class LanguageCodes
{
    public static readonly IReadOnlySet<string> Allowed = new HashSet<string>(StringComparer.Ordinal)
    {
         // Nordiska
        "sv", "no", "da", "fi",
        // Övriga europeiska
        "en", "de", "fr", "es", "it", "nl", "pl", "pt",
        // Asiatiska och övriga
        "zh", "ja", "ko", "hi", "ar", "tr"
    };
}
