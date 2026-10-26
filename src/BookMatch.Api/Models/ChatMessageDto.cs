namespace BookMatch.Api.Models;

// Ett meddelande i historiken
public record ChatMessageDto(ChatMessageRole Role, string Content);

