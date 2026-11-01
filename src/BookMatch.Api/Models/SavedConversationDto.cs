namespace BookMatch.Api.Models;

// Sparad konversation till frontend
public record SavedConversationDto(
    string Language,
    Genre Genre,
    ReadingLevel Level,
    IReadOnlyList<ChatMessageDto> History);
