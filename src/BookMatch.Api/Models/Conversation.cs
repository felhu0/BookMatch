namespace BookMatch.Api.Models;

public class Conversation
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public required string Language { get; set; }
    public Genre Genre { get; set; }
    public ReadingLevel Level { get; set; }
    public DateTime UpdatedAt { get; set; }           // senast sparad (UTC, Coordinated Universal Time)
    public List<ConversationMessage> Messages { get; set; } = [];
}
