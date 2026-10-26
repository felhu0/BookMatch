namespace BookMatch.Api.Models;

// Anrop från formuläret (DTO, Data Transfer Object)
public record ChatRequest(
    string Language,    // språkkod enligt ISO 639-1 (tvåbokstavskod, t.ex. "sv")
    Genre Genre,
    ReadingLevel Level,
    IReadOnlyList<ChatMessageDto>? History = null,
    string? Message = null);
